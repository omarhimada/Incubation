using Incubation.Functions;
using Incubation.Models;
using Incubation.Storage;
using System.Diagnostics;
using static Incubation.Configuration;

/// <summary>
/// Provides functionality for generating large-scale synthetic customer and transaction datasets with realistic
/// behavioral and telecom attributes for testing, analysis, or demonstration purposes.
/// </summary>
/// <remarks>The Incubate class simulates customer profiles, purchasing patterns, and telecom-related fields using
/// randomized distributions and configurable parameters to closely mimic real-world data. It is intended for scenarios
/// where representative, varied datasets are required, such as machine learning training, system performance testing,
/// or analytics development. The generated data includes correlations and distributions designed to reflect actual
/// usage patterns, supporting more effective evaluation of downstream systems.</remarks>
public sealed class Incubate {
    private readonly ITrainingDataStorage _trainingDataStorage;

    public Incubate(ITrainingDataStorage trainingDataStorage) {
        _trainingDataStorage = trainingDataStorage;
    }

    /// <summary>
    /// Uses poisson, normal, log-normal, weighted choice, and other random distributions to generate realistic
    /// randomness as opposed to relying on the built-in System.Random alone.
    /// </summary>
    internal readonly Random _random = RandEx.CreateRandom();

    /// <summary>
    /// Represents the number of SKU clusters used for internal grouping.
    /// </summary>
    internal const int SkuClusters = 12;

    /// <summary>
    /// Represents the maximum number of days in the past that can be considered, typically used as a limit for
    /// date-based operations.
    /// </summary>
    internal const int MaxDaysBack = 365 * 4;

    internal List<string>? _zipfSku;
    internal double[]? _zipfWeights;

    private async Task OutputData(ManyRandomTransactions transactions, ManyRandomCustomers customers) {
        await _trainingDataStorage.WriteNewRandomOrdersCsvAsync(transactions);
        await _trainingDataStorage.WriteNewRandomCustomersCsvAsync(customers.Customers, includePiiColumns: false);
    }

    /// <summary>
    /// Generates a random email address using a randomly created prefix and a randomly selected suffix.
    /// </summary>
    /// <returns>A string containing a randomly generated email address.</returns>
    private string RandomEmail() {
        string prefix = new([.. Enumerable
            .Repeat(AvailableCharsForRandomEmailPrefixes, 16)
            .Select(s => s[_random.Next(s.Length)])]);

        char suffix = AvailableCharsForRandomEmailPrefixes[_random.Next(AvailableCharsForRandomEmailPrefixes.Length)];
        return $"{prefix}{suffix}";
    }

    /// <summary>
    /// Returns a random Boolean value, where the probability of returning true is specified by the given parameter.
    /// </summary>
    /// <param name="probability">The probability of returning <see langword="true"/>. Must be between 0.0 and 1.0, inclusive. The default is 0.5.</param>
    /// <returns><see langword="true"/> with the specified probability; otherwise, <see langword="false"/>.</returns>
    private bool RandomBoolean(double probability = 0.5) => _random.Chance(probability);

    /// <summary>
    /// Generates a random area code and a corresponding 10-digit phone number using the available digits.
    /// </summary>
    /// <returns>A tuple containing the area code as a three-digit string and the full 10-digit phone number.</returns>
    private (string areaCode, string phoneNumber) RandomAreaCodeAndPhoneNumber() {
        string phoneNumber = new([.. Enumerable
            .Repeat(AvailableDigitsForPhoneNumbers, 10)
            .Select(s => s[_random.Next(s.Length)])]);

        return (phoneNumber[..3], phoneNumber);
    }

    /// <summary>
    /// Generates a random past date, with a higher probability of selecting a date from the recent past.
    /// </summary>
    /// <remarks>This method selects a date from the last 90 days with a 70% probability, using a normal
    /// distribution biased toward more recent dates. Otherwise, it selects a date from the full available history. This
    /// is useful for generating test data that more closely resembles real-world usage patterns, where recent dates are
    /// more common.</remarks>
    /// <returns>A <see cref="DateTime"/> value representing a randomly selected date in the past. The date is more likely to be
    /// within the last 90 days, but may be any date up to the maximum allowed history.</returns>
    private DateTime RandomDateTimeRecentBiased() {
        DateTime today = DateTime.Today;

        // 70% recent (last 90 days), 30% anywhere in history
        if (_random.Chance(0.70)) {
            int days = (int)Math.Abs(_random.NextGaussian(mean: 30, stdDev: 25));
            days = (int)((double)days)._clamp(0, 90);
            return today.AddDays(-days);
        }

        int back = _random.Next(0, MaxDaysBack);
        return today.AddDays(-back);
    }

    /// <summary>
    /// Calculates the cluster index for the specified SKU string.
    /// </summary>
    /// <remarks>The returned cluster index is determined by hashing the SKU string and mapping it to a value
    /// in the range [0, SkuClusters). This method ensures that the same SKU will always map to the same cluster
    /// index.</remarks>
    /// <param name="sku">The SKU identifier to map to a cluster. Cannot be null.</param>
    /// <returns>An integer representing the cluster index corresponding to the specified SKU.</returns>
    private int SkuToCluster(string sku) {
        unchecked {
            int h = 17;
            for (int i = 0; i < sku.Length; i++) {
                h = (h * 31) + sku[i];
            }
            h = Math.Abs(h);
            return h % SkuClusters;
        }
    }

    /// <summary>
    /// Initializes SKU popularity weights according to a Zipf distribution and shuffles the SKU list to randomize
    /// popularity assignments.
    /// </summary>
    /// <remarks>This method prepares internal data structures for simulating SKU popularity based on a Zipf
    /// distribution, which is commonly used to model real-world item frequency distributions. The input list is
    /// shuffled to ensure that the most popular SKUs are not always the same across runs.</remarks>
    /// <param name="allSku">The complete list of SKU identifiers to be assigned Zipf-distributed popularity weights. Cannot be null.</param>
    private void BuildZipfSkus(List<string> allSku) {
        int n = allSku.Count;
        double s = 1.07;

        // Shuffle once so “popular” isn’t always the same prefix
        var shuffled = allSku.OrderBy(_ => _random.Next()).ToList();
        var weights = new double[n];

        for (int i = 0; i < n; i++) {
            int rank = i + 1;
            weights[i] = 1.0 / Math.Pow(rank, s);
        }

        _zipfSku = shuffled;
        _zipfWeights = weights;
    }

    /// <summary>
    /// Builds and returns a dictionary of simulated people, each assigned a unique identifier and randomized attributes
    /// based on customer segments.
    /// </summary>
    /// <remarks>The generated people reflect a distribution of customer segments and behavioral traits,
    /// suitable for use in simulations or modeling scenarios. The number of people and their identifiers are determined
    /// by the current configuration. Attribute values are randomized within segment-appropriate ranges to provide
    /// realistic diversity among simulated customers.</remarks>
    /// <returns>A dictionary mapping person identifiers to <see cref="Person"/> instances, each representing a simulated
    /// customer with segment-specific characteristics.</returns>
    private Dictionary<int, Person> BuildPeople() {
        Dictionary<int, Person> people = new(capacity: MaximumNumberOfCustomers);

        for (int id = StartCustomersAt; id < StartCustomersAt + MaximumNumberOfCustomers; id++) {
            PersonSegment seg = _random.WeightedChoice(new (PersonSegment, double)[] {
                (PersonSegment.Steady,     0.50),
                (PersonSegment.DealSeeker, 0.20),
                (PersonSegment.HighValue,  0.15),
                (PersonSegment.AtRisk,     0.15)
            });

            double baseLambda = seg switch {
                PersonSegment.HighValue => _random.NextLogNormal(mu: 2.2, sigma: 0.4),
                PersonSegment.Steady => _random.NextLogNormal(mu: 1.7, sigma: 0.45),
                PersonSegment.DealSeeker => _random.NextLogNormal(mu: 1.4, sigma: 0.55),
                PersonSegment.AtRisk => _random.NextLogNormal(mu: 1.0, sigma: 0.60),
                _ => _random.NextLogNormal(mu: 1.6, sigma: 0.5)
            };

            baseLambda._clamp(0.2, 80);

            double basketMean = (seg switch {
                PersonSegment.HighValue => 3.2,
                PersonSegment.DealSeeker => 2.8,
                PersonSegment.Steady => 2.2,
                PersonSegment.AtRisk => 1.7,
                _ => 2.2
            } + _random.NextGaussian(0, 0.25))._clamp(1.0, 6.0);

            double priceSensitivity = (seg switch {
                PersonSegment.DealSeeker => 1.2,
                PersonSegment.AtRisk => 1.0,
                PersonSegment.Steady => 0.9,
                PersonSegment.HighValue => 0.6,
                _ => 0.9
            } + _random.NextGaussian(0, 0.15))._clamp(0.3, 1.6);

            double intlAffinity = (seg switch {
                PersonSegment.HighValue => 0.65,
                PersonSegment.Steady => 0.35,
                PersonSegment.DealSeeker => 0.25,
                PersonSegment.AtRisk => 0.15,
                _ => 0.3
            } + _random.NextGaussian(0, 0.12))._clamp(0.0, 1.0);

            double supportNeed = (seg switch {
                PersonSegment.AtRisk => 0.75,
                PersonSegment.DealSeeker => 0.45,
                PersonSegment.Steady => 0.30,
                PersonSegment.HighValue => 0.25,
                _ => 0.35
            } + _random.NextGaussian(0, 0.10))._clamp(0.0, 1.0);

            int preferredCluster = _random.Next(0, SkuClusters);

            people[id] = new Person(
                PersonId: id,
                Segment: seg,
                BaseOrderLambda: baseLambda,
                BasketMean: basketMean,
                PriceSensitivity: priceSensitivity,
                InternationalAffinity: intlAffinity,
                SupportNeediness: supportNeed,
                PreferredSkuCluster: preferredCluster
            );
        }

        return people;
    }

    /// <summary>
    /// Asynchronously generates synthetic customer and transaction data, simulating realistic purchasing and telecom
    /// behaviors for use in testing or analysis scenarios.
    /// </summary>
    /// <remarks>The generated data includes randomized transactions, customer profiles, and telecom-related fields
    /// with realistic distributions and correlations. The method writes the resulting data to the configured output
    /// destination. This method is intended for use in scenarios where large, varied datasets are needed for
    /// development, testing, or demonstration purposes. The operation may take significant time depending on the
    /// configured data volume.</remarks>
    /// <returns>A task that represents the asynchronous data generation operation.</returns>
    public async Task GenerateData() {
        Stopwatch sw = Stopwatch.StartNew();

        // Build SKUs (000..99 for MaximumLengthOfSku=2)
        // If you want “010” etc, increase MaximumLengthOfSku.
        List<string> allSku =
            [.. Enumerable.Range(1, MaximumLengthOfSku)
                .SelectMany(count => PartialCombinatorial.CartesianProduct(CharactersToUse, count))
                .Select(chars => new string(chars))];

        BuildZipfSkus(allSku);
        Dictionary<int, Person> people = BuildPeople();

        var customerIds = people.Keys.ToArray();
        var customerWeights = customerIds.Select(id => people[id].BaseOrderLambda).ToArray();

        // Transactions
        ManyRandomTransactions manyOrders = new();

        for (long i = 0; i < OrdersToGenerate; i++) {
            int custId = customerIds[_random.WeightedIndex(customerWeights)];
            Person person = people[custId];

            DateTime dt = RandomDateTimeRecentBiased();
            double season = dt.Month switch {
                11 or 12 => 1.25,
                1 => 1.10,
                2 => 0.85,
                6 or 7 => 1.05,
                _ => 1.0
            };

            int items = _random.NextPoisson(person.BasketMean * season);
            items = Math.Clamp(items, 1, MaximumCartItemQuantity);

            List<Product> cartItems = new(items);

            for (int k = 0; k < items; k++) {
                // Sample a few candidates and pick the one that best matches preference.
                string bestSku = _zipfSku![_random.WeightedIndex(_zipfWeights!)];
                double bestScore = double.NegativeInfinity;

                for (int tries = 0; tries < 3; tries++) {
                    string sku = _zipfSku![_random.WeightedIndex(_zipfWeights!)];
                    int cluster = SkuToCluster(sku);
                    double prefBoost = (cluster == person.PreferredSkuCluster) ? 0.8 : 0.0;
                    double score = prefBoost + _random.NextGaussian(0, 0.25);

                    if (score > bestScore) { bestScore = score; bestSku = sku; }
                }

                double qMean = (2.0 / person.PriceSensitivity)._clamp(0.8, 3.0);
                int qty = Math.Clamp(1 + _random.NextPoisson(qMean), 1, MaximumSkuQuantity);

                cartItems.Add(new Product { Sku = bestSku, Quantity = qty });
            }

            manyOrders.Transactions.Add(new Transaction {
                CustomerId = custId,
                OrderDate = dt,
                Produdcts = cartItems
            });
        }

        List<Customer> customers =
            [.. from order in manyOrders.Transactions
               group order by order.CustomerId into grouped
               select new Customer {
                   CustomerId = grouped.Key,
                   AccountLength = (float)(DateTime.Today - grouped.Min(i => i.OrderDate)).TotalDays,
                   DaysSinceLastPurchase = (float)(DateTime.Today - grouped.Max(i => i.OrderDate)).TotalDays,
                   Email = RandomEmail()
               }];

        // Add correlated telecom-ish fields, churn probabilistic
        foreach (Customer customer in customers) {
            Person person = people[(int)customer.CustomerId];

            // Missingness like real life
            if (_random.Chance(0.015))
                customer.Email = null;

            (string? areaCode, string? phoneNumber) = RandomAreaCodeAndPhoneNumber();
            customer.AreaCode = areaCode;
            customer.PhoneNumber = _random.Chance(0.01) ? null : phoneNumber;

            customer.State = _random.WeightedChoice(new (string, double)[] {
                ("CA", 0.13), ("TX", 0.09), ("FL", 0.07), ("NY", 0.06), ("PA", 0.04),
                ("IL", 0.04), ("OH", 0.04), ("GA", 0.03), ("NC", 0.03), ("MI", 0.03),
                ("WA", 0.025), ("AZ", 0.02), ("MA", 0.02), ("VA", 0.02), ("NJ", 0.02),
                ("OTHER", 0.405)
            });

            customer.Voice = person.Segment switch {
                PersonSegment.AtRisk => RandomBoolean(0.70),
                PersonSegment.DealSeeker => RandomBoolean(0.80),
                PersonSegment.Steady => RandomBoolean(0.85),
                PersonSegment.HighValue => RandomBoolean(0.90),
                _ => RandomBoolean(0.83)
            };

            double acct = Math.Max(1.0, customer.AccountLength);
            bool outlier = _random.Chance(0.006);

            customer.NumberOfMessages = (float)(_random.NextLogNormal(mu: 2.4, sigma: 0.7) * (acct / 365.0))._clamp(0, 15000);

            double supportBase = 0.5 + 6.0 * person.SupportNeediness + 0.012 * customer.DaysSinceLastPurchase;
            
            if (outlier) supportBase *= 2.0;
            
            customer.NumberOfCustomerServiceCalls = (float)Math.Clamp(_random.NextPoisson(supportBase), 0, 40);

            if (customer.Voice) {
                int x = _random.NextPoisson(lambda: (acct / 30.0) * (1.2 - 0.4 * person.PriceSensitivity));
                double dayCalls = _random.NextPoisson(lambda: (acct / 30.0) * (1.2 - 0.4 * person.PriceSensitivity))._clamp(0, 300);
                double eveCalls = _random.NextPoisson(lambda: (acct / 33.0) * (1.1 - 0.3 * person.PriceSensitivity))._clamp(0, 300);
                double nightCalls = _random.NextPoisson(lambda: (acct / 40.0) * (0.9 - 0.2 * person.PriceSensitivity))._clamp(0, 250);

                if (outlier) { dayCalls *= 1.8; eveCalls *= 1.8; nightCalls *= 1.8; }

                customer.TotalDaytimeCalls = (float)dayCalls;
                customer.TotalEveningCalls = (float)eveCalls;
                customer.TotalNightCalls = (float)nightCalls;

                double dayMinPerCall = _random.NextLogNormal(mu: 1.8, sigma: 0.35);
                double eveMinPerCall = _random.NextLogNormal(mu: 1.9, sigma: 0.35);
                double nightMinPerCall = _random.NextLogNormal(mu: 1.6, sigma: 0.40);

                double dayMinutes = (dayCalls * dayMinPerCall)._clamp(0, 20000);
                double eveMinutes = (eveCalls * eveMinPerCall)._clamp(0, 20000);
                double nightMinutes = (nightCalls * nightMinPerCall)._clamp(0, 20000);

                dayMinutes = (dayMinutes * (1 + _random.NextGaussian(0, 0.08)))._clamp(0, 20000);
                eveMinutes = (eveMinutes * (1 + _random.NextGaussian(0, 0.08)))._clamp(0, 20000);
                nightMinutes = (nightMinutes * (1 + _random.NextGaussian(0, 0.10)))._clamp(0, 20000);

                customer.TotalDaytimeMinutes = (float)dayMinutes;
                customer.TotalEveningMinutes = (float)eveMinutes;
                customer.TotalNightMinutes = (float)nightMinutes;

                double dayRate = 0.17 + _random.NextGaussian(0, 0.01);
                double eveRate = 0.10 + _random.NextGaussian(0, 0.01);
                double nightRate = 0.06 + _random.NextGaussian(0, 0.008);

                customer.TotalDaytimeCharges = (float)(dayMinutes * dayRate * (1 + _random.NextGaussian(0, 0.03)))._clamp(0, 5000);
                customer.TotalEveningCharges = (float)(eveMinutes * eveRate * (1 + _random.NextGaussian(0, 0.03)))._clamp(0, 5000);
                customer.TotalNightCharges = (float)(nightMinutes * nightRate * (1 + _random.NextGaussian(0, 0.03)))._clamp(0, 5000);
            }

            bool likelyIntl = _random.NextDouble() < (0.05 + 0.65 * person.InternationalAffinity);
            customer.InternationalPlan = likelyIntl && _random.Chance(0.70 + 0.20 * person.InternationalAffinity);

            if (customer.InternationalPlan) {
                double intlCalls = _random.NextPoisson(lambda: 2 + 20 * person.InternationalAffinity)._clamp(0, 200);
                double intlMinPerCall = _random.NextLogNormal(mu: 1.6, sigma: 0.45);
                double intlMinutes = (intlCalls * intlMinPerCall)._clamp(0, 8000);
                if (outlier)
                    intlMinutes *= 2.2;

                customer.TotalInternationalCalls = (float)intlCalls;
                customer.TotalInternationalMinutes = (float)intlMinutes;

                double intlRate = 0.28 + _random.NextGaussian(0, 0.02);
                customer.TotalInternationalCharges = (float)(intlMinutes * intlRate * (1 + _random.NextGaussian(0, 0.04)))._clamp(0, 6000);
            }

            // Probabilistic churn (instead hardcoded thresholds)
            double usage =
                customer.TotalDaytimeMinutes +
                customer.TotalEveningMinutes +
                customer.TotalNightMinutes +
                customer.TotalInternationalMinutes;

            double usageNorm = Math.Log(1 + usage);
            double support = customer.NumberOfCustomerServiceCalls;

            double baseline = person.Segment switch {
                PersonSegment.AtRisk => 0.9,
                PersonSegment.DealSeeker => 0.2,
                PersonSegment.Steady => -0.2,
                PersonSegment.HighValue => -0.5,
                _ => 0.0
            };

            // Logistic model for churn probability
            double z =
                baseline
                + 0.045 * customer.DaysSinceLastPurchase
                + 0.18 * support
                - 0.30 * usageNorm
                - (customer.InternationalPlan ? 0.25 : 0.0)
                + _random.NextGaussian(0, 0.35);

            // Convert to probability via sigmoid
            double probabilityChurn = RandEx.Sigmoid(z)._clamp(0.01, 0.99);
            customer.Churned = _random.Chance(probabilityChurn);

            // Small consistency changes for the sake of helps label realism
            if (customer.Churned && customer.DaysSinceLastPurchase < 10 && _random.Chance(0.7)) {
                customer.DaysSinceLastPurchase = (customer.DaysSinceLastPurchase + _random.Next(5, 45))._clamp(0, 3650);
            }
            if (!customer.Churned && customer.DaysSinceLastPurchase > 45 && _random.Chance(0.5)) {
                customer.DaysSinceLastPurchase = (customer.DaysSinceLastPurchase - _random.Next(10, 40))._clamp(0, 3650);
            }
        }

        ManyRandomCustomers randomCustomes= new() { Customers = customers };

        sw.Stop();
        await OutputData(manyOrders, randomCustomes);

        Console.WriteLine($"Generated {OrdersToGenerate:N0} transactions and {customers.Count:N0} customers in {sw.Elapsed.TotalSeconds:N2}s");
    }
}


public static class Program {
    public static async Task Main(string[] args) {
        // Change output dir or swap TrainingDataStorage for your real TrainingDataStorage.

        string outDir = args.Length > 0 ? args[0] : Path.Combine(Environment.CurrentDirectory, "out");
        ITrainingDataStorage storage = new LocalTrainingDataStorage(outDir);

        Incubate incubate = new(storage);
        await incubate.GenerateData();
    }
}

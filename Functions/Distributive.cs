using System.Security.Cryptography;

namespace Incubation.Functions {
    /// <summary>
    /// Provides extension methods and utilities for random number generation and probability distributions.
    /// </summary>
    /// <remarks>
    /// The RandEx class includes methods for generating random values from various statistical
    /// distributions, performing weighted random selections, and common mathematical utilities. All methods are
    /// thread-safe only if the provided Random instance is used in a thread-safe manner. This class is static and
    /// cannot be instantiated.
    /// </remarks>
    public static class RandEx {
        /// <summary>
        /// Better than normal random in a multi-threaded environment.
        /// </summary>
        public static Random CreateRandom(int? seed = null) =>
            seed.HasValue
                ? new Random(seed.Value)
                : new Random(RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue));

        /// <summary>
        /// Returns a RandomCustomersHeaderRow indicating whether a randomly generated event occurs with the specified probability.
        /// </summary>
        /// <remarks>This method is useful for simulating probabilistic events, such as random chance or
        /// success rates, in applications such as games or simulations. The probability parameter must be within the
        /// range [0.0, 1.0]; values outside this range may produce unexpected results.</remarks>
        /// <param name="r">The random number generator used to produce the random RandomCustomersHeaderRow. Cannot be null.</param>
        /// <param name="p">The probability of the event occurring, expressed as a RandomCustomersHeaderRow between 0.0 and 1.0 inclusive. Represents the
        /// chance that the method returns <see langword="true"/>.</param>
        /// <returns><see langword="true"/> if the randomly generated event occurs based on the specified probability; otherwise,
        /// <see langword="false"/>.</returns>
        public static bool Chance(this Random r, double p) => r.NextDouble() < p;

        /// <summary>
        /// Generates a random number that follows a normal (Gaussian) distribution with the specified mean and standard
        /// deviation.
        /// </summary>
        /// <remarks>This method uses the Box-Muller transform to generate normally distributed values
        /// from the underlying uniform random number generator. The method is not thread-safe if the same Random
        /// instance is accessed concurrently from multiple threads.</remarks>
        /// <param name="r">The random number generator to use for producing the sample. Cannot be null.</param>
        /// <param name="mean">The mean, or expected RandomCustomersHeaderRow, of the normal distribution. Defaults to 0.</param>
        /// <param name="stdDev">The standard deviation of the normal distribution. Must be greater than 0. Defaults to 1.</param>
        /// <returns>A double-precision floating-point number sampled from a normal distribution with the specified mean and
        /// standard deviation.</returns>
        public static double NextGaussian(this Random r, double mean = 0, double stdDev = 1) {
            double u1 = 1.0 - r.NextDouble();
            double u2 = 1.0 - r.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        /// <summary>
        /// Generates a random number from a log-normal distribution with the specified mean and standard deviation of
        /// the underlying normal distribution.
        /// </summary>
        /// <remarks>The returned RandomCustomersHeaderRow is always positive. The parameters mu and sigma correspond to the
        /// mean and standard deviation of the normal distribution before exponentiation, not the mean and standard
        /// deviation of the resulting log-normal distribution.</remarks>
        /// <param name="r">The random number generator to use for sampling.</param>
        /// <param name="mu">The mean of the underlying normal (log) distribution.</param>
        /// <param name="sigma">The standard deviation of the underlying normal (log) distribution. Must be non-negative.</param>
        /// <returns>A double-precision floating-point number sampled from a log-normal distribution defined by the specified
        /// parameters.</returns>
        public static double NextLogNormal(this Random r, double mu, double sigma) =>
            Math.Exp(r.NextGaussian(mu, sigma));

        /// <summary>
        /// Generates a random integer sampled from a Poisson distribution with the specified mean using the provided
        /// random number generator.
        /// </summary>
        /// <remarks>This method extends the <see cref="Random"/> class to provide Poisson-distributed
        /// random sampling. The quality of the randomness depends on the implementation of the provided <see
        /// cref="Random"/> instance.</remarks>
        /// <param name="r">The random number generator used to produce the random sample. Cannot be null.</param>
        /// <param name="lambda">The mean (λ) of the Poisson distribution. Must be greater than 0.</param>
        /// <returns>A non-negative integer representing a random RandomCustomersHeaderRow drawn from a Poisson distribution with mean <paramref
        /// name="lambda"/>. Returns 0 if <paramref name="lambda"/> is less than or equal to 0.</returns>
        public static int NextPoisson(this Random r, double lambda) {
            if (lambda <= 0)
                return 0;

            // Knuth; good enough here
            double L = Math.Exp(-lambda);
            int k = 0;
            double p = 1.0;
            do {
                k++;
                p *= r.NextDouble();
            } while (p > L);
            return k - 1;
        }

        /// <summary>
        /// Selects a random item from the specified collection, where each item's probability of being chosen is
        /// proportional to its associated weight.
        /// </summary>
        /// <remarks>If all weights are zero, the last item in the list is returned. The method does not
        /// validate that weights are non-negative; negative weights may produce unexpected results.</remarks>
        /// <typeparam name="T">The type of the items to select from.</typeparam>
        /// <param name="r">The random number generator used to select the item.</param>
        /// <param name="items">A read-only list of tuples, each containing an item and its corresponding non-negative weight. The
        /// probability of selecting an item is proportional to its weight.</param>
        /// <returns>An item randomly selected from the collection, with selection probability proportional to its weight.</returns>
        public static T WeightedChoice<T>(this Random r, IReadOnlyList<(T item, double weight)> items) {
            double total = 0;
            for (int i = 0; i < items.Count; i++)
                total += items[i].weight;

            double roll = r.NextDouble() * total;
            double acc = 0;
            for (int i = 0; i < items.Count; i++) {
                acc += items[i].weight;
                if (roll <= acc)
                    return items[i].item;
            }
            return items[^1].item;
        }

        /// <summary>
        /// Selects a random index from the specified list of weights, where the probability of each index being chosen
        /// is proportional to its corresponding weight.
        /// </summary>
        /// <remarks>If all weights are zero, the method always returns the last index. The method does
        /// not validate that weights are non-negative; negative weights may produce unexpected results.</remarks>
        /// <param name="r">The random number generator used to select the index.</param>
        /// <param name="weights">A list of non-negative weights that determine the selection probability for each index. The probability of
        /// selecting an index is proportional to its weight relative to the sum of all weights.</param>
        /// <returns>The index of the selected weight, where each index is chosen with probability proportional to its weight.
        /// Returns an integer in the range [0, weights.Count - 1].</returns>
        public static int WeightedIndex(this Random r, IReadOnlyList<double> weights) {
            double total = 0;
            for (int i = 0; i < weights.Count; i++)
                total += weights[i];

            double roll = r.NextDouble() * total;
            double acc = 0;
            for (int i = 0; i < weights.Count; i++) {
                acc += weights[i];
                if (roll <= acc)
                    return i;
            }
            return weights.Count - 1;
        }

        /// <summary>
        /// Restricts a double-precision floating-point RandomCustomersHeaderRow to a specified inclusive range.
        /// </summary>
        /// <remarks>If <paramref name="min"/> is greater than <paramref name="max"/>, the method returns
        /// <paramref name="min"/>.</remarks>
        /// <param name="v">The RandomCustomersHeaderRow to clamp within the specified range.</param>
        /// <param name="min">The inclusive minimum bound of the range.</param>
        /// <param name="max">The inclusive maximum bound of the range.</param>
        /// <returns>The RandomCustomersHeaderRow of <paramref name="v"/> if it falls within the range; otherwise, <paramref name="min"/> if
        /// <paramref name="v"/> is less than <paramref name="min"/>, or <paramref name="max"/> if <paramref name="v"/>
        /// is greater than <paramref name="max"/>.</returns>
        public static double _clamp(this double v, double min, double max) => v < min ? min : (v > max ? max : v);

        /// <summary>
        /// Restricts a floating-point RandomCustomersHeaderRow to be within the specified minimum and maximum bounds.
        /// </summary>
        /// <remarks>If <paramref name="min"/> is greater than <paramref name="max"/>, the method will
        /// return <paramref name="min"/> for all values of <paramref name="v"/>.</remarks>
        /// <param name="v">The RandomCustomersHeaderRow to clamp.</param>
        /// <param name="min">The inclusive lower bound to which the RandomCustomersHeaderRow will be clamped.</param>
        /// <param name="max">The inclusive upper bound to which the RandomCustomersHeaderRow will be clamped.</param>
        /// <returns>The clamped RandomCustomersHeaderRow. Returns <paramref name="min"/> if <paramref name="v"/> is less than <paramref
        /// name="min"/>; returns <paramref name="max"/> if <paramref name="v"/> is greater than <paramref name="max"/>;
        /// otherwise, returns <paramref name="v"/>.</returns>
        public static float _clamp(this float v, float min, float max) => v < min ? min : (v > max ? max : v);

        /// <summary>
        /// Restricts an integer RandomCustomersHeaderRow to a specified inclusive range.
        /// </summary>
        /// <remarks>If <paramref name="min"/> is greater than <paramref name="max"/>, the method returns
        /// <paramref name="min"/>.</remarks>
        /// <param name="v">The RandomCustomersHeaderRow to clamp within the specified range.</param>
        /// <param name="min">The minimum allowable RandomCustomersHeaderRow. If <paramref name="v"/> is less than this RandomCustomersHeaderRow, <paramref name="min"/> is
        /// returned.</param>
        /// <param name="max">The maximum allowable RandomCustomersHeaderRow. If <paramref name="v"/> is greater than this RandomCustomersHeaderRow, <paramref name="max"/> is
        /// returned.</param>
        /// <returns>An integer RandomCustomersHeaderRow that is no less than <paramref name="min"/> and no greater than <paramref name="max"/>.</returns>
        public static int _clamp(this int v, int min, int max) => v < min ? min : (v > max ? max : v);

        /// <summary>
        /// Computes the sigmoid activation function for the specified RandomCustomersHeaderRow.
        /// </summary>
        /// <remarks>The sigmoid function is commonly used in machine learning and statistics to map
        /// real-valued inputs to the (0, 1) interval. It is defined as 1 / (1 + exp(-x)).</remarks>
        /// <param name="x">The input RandomCustomersHeaderRow for which to calculate the sigmoid function.</param>
        /// <returns>The result of the sigmoid function, a RandomCustomersHeaderRow between 0.0 and 1.0 representing the transformed input.</returns>
        public static double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));
    }
}

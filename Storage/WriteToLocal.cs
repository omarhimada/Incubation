using Incubation.Models;
using System.Globalization;
using System.Text;

namespace Incubation.Storage {
    /// <summary>
    /// Provides local file system storage for training data, including writing customer and order data as CSV files for
    /// use in machine learning workflows. This can later get streamed if you prefer to use cloud storage solutions.
    /// </summary>
    /// <remarks>
    /// This class creates and manages CSV files in a specified directory, enabling the export of
    /// training datasets in a format suitable for data analysis or model training. All files are written using UTF-8
    /// encoding. The class is not thread-safe; concurrent access should be managed externally if used in multi-threaded
    /// scenarios.
    /// </remarks>
    public sealed class LocalTrainingDataStorage : ITrainingDataStorage {
        private readonly string _dir;
        private const string RandomOrdersHeaderRow = "CustomerId,Sku,Quantity,RecencyDays";
        private const string RandomCustomersHeaderRow ="CustomerId,Email,PhoneNumber,Churned,State,AreaCode,AccountLength,DaysSinceLastPurchase,NumberOfMessages,TotalDaytimeMinutes,TotalDaytimeCalls,TotalDaytimeCharges,TotalEveningMinutes,TotalEveningCalls,TotalEveningCharges,TotalNightMinutes,TotalNightCalls,TotalNightCharges,TotalInternationalMinutes,TotalInternationalCalls,TotalInternationalCharges,NumberOfCustomerServiceCalls,InternationalPlan,Voice";

        public LocalTrainingDataStorage(string dir) {
            _dir = dir;
            Directory.CreateDirectory(_dir);
        }

        /// <summary>
        /// Writes the provided collection of random orders to a new CSV file in the configured directory. The file name
        /// includes the current UTC timestamp to ensure uniqueness.
        /// </summary>
        /// <remarks>Each order is written as a line in the CSV file with columns for customer ID, order
        /// recency in days, and a pipe-delimited list of products in the format 'sku:quantity'. The method completes
        /// synchronously and does not perform actual asynchronous I/O.</remarks>
        /// <param name="orders">The collection of random transactions to write to the CSV file. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public Task WriteNewRandomOrdersCsvAsync(ManyRandomTransactions orders) {
            string path = Path.Combine(_dir, $"orders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            using var sw = new StreamWriter(path, false, Encoding.UTF8);
            sw.WriteLine(RandomOrdersHeaderRow);

            List<Order> allOrders = [];
            foreach (Transaction transaction in orders.Transactions) {
                float recencyDays = (float)(DateTime.UtcNow - transaction.OrderDate).TotalDays;

                // Merge duplicate SKUs inside the same order (basket amplification)
                var merged =
                    transaction.Produdcts
                     .GroupBy(p => p.Sku)
                     .Select(g => new {
                         Sku = g.Key,
                         Quantity = g.Sum(x => x.Quantity)
                     })
                     .Where(x => x.Quantity > 0)
                     .OrderBy(x => x.Sku)
                     .ToList();

                IEnumerable<Order> items =
                    merged.Select(x => new Order() {
                        Quantity = x.Quantity,
                        Sku = x.Sku,
                        CustomerId = transaction.CustomerId,
                        RecencyDays = recencyDays
                    });

                allOrders.AddRange(items);
            }

            foreach (Order order in allOrders) {
                sw.WriteLine(string.Join(",", order.CustomerId, order.Sku, order.Quantity, order.RecencyDays));
            }

            Console.WriteLine($"Wrote: {path}");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Writes the specified list of customers to a new CSV file in the configured directory, using a timestamped
        /// filename. Optionally includes personally identifiable information (PII) columns in the output.
        /// </summary>
        /// <remarks>The generated CSV file includes a header row and is saved with a filename in the
        /// format 'customers_yyyyMMdd_HHmmss.csv' in the target directory. The column order matches the expected order
        /// for loading data. If includePiiColumns is false, the email and phone number columns will be empty.</remarks>
        /// <param name="customers">The list of customers to write to the CSV file. Each customer in the list will be represented as a row in
        /// the output.</param>
        /// <param name="includePiiColumns">true to include PII columns such as email and phone number in the CSV output; otherwise, false to omit these
        /// columns.</param>
        /// <returns>A task that represents the asynchronous write operation. The task is completed when the CSV file has been
        /// written.</returns>
        public Task WriteNewRandomCustomersCsvAsync(List<Customer> customers, bool includePiiColumns) {
            string path = Path.Combine(_dir, $"customers_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            using StreamWriter sw = new(path, false, Encoding.UTF8);

            // Keep columns aligned with your LoadColumn order.
            sw.WriteLine(RandomCustomersHeaderRow);

            const string _true = "true";
            const string _false = "false";

            foreach (Customer c in customers) {
                string? email = includePiiColumns ? c.Email : null;
                string? phone = includePiiColumns ? c.PhoneNumber : null;

                sw.WriteLine(string.Join(",", new[] {
                    c.CustomerId.ToString(CultureInfo.InvariantCulture),
                    CsvEscape(email),
                    CsvEscape(phone),
                    c.Churned ? _true : _false,
                    CsvEscape(c.State),
                    CsvEscape(c.AreaCode),
                    F(c.AccountLength),
                    F(c.DaysSinceLastPurchase),
                    F(c.NumberOfMessages),

                    F(c.TotalDaytimeMinutes),
                    F(c.TotalDaytimeCalls),
                    F(c.TotalDaytimeCharges),

                    F(c.TotalEveningMinutes),
                    F(c.TotalEveningCalls),
                    F(c.TotalEveningCharges),

                    F(c.TotalNightMinutes),
                    F(c.TotalNightCalls),
                    F(c.TotalNightCharges),

                    F(c.TotalInternationalMinutes),
                    F(c.TotalInternationalCalls),
                    F(c.TotalInternationalCharges),

                    F(c.NumberOfCustomerServiceCalls),
                    c.InternationalPlan ? _true : _false,
                    c.Voice ? _true : _false,
                }));
            }

            Console.WriteLine($"Wrote: {path}");
            return Task.CompletedTask;

            static string F(float x) => x.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string CsvEscape(string? s) {
            if (string.IsNullOrEmpty(s))
                return "";
            bool mustQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
            if (!mustQuote)
                return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
    }
}

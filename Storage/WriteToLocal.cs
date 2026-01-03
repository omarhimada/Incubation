using Incubation.Models;
using System;
using System.Collections.Generic;
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

        public LocalTrainingDataStorage(string dir) {
            _dir = dir;
            Directory.CreateDirectory(_dir);
        }

        public Task WriteNewRandomOrdersCsvAsync(ManyRandomTransactions orders) {
            string path = Path.Combine(_dir, $"orders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            using StreamWriter sw = new(path, false, Encoding.UTF8);

            const string _ordersHeader = "CustomerId,OrderDate,Produdcts";

            sw.WriteLine(_ordersHeader); // Produdcts as sku:qty|sku:qty|...
            foreach (var o in orders.Transactions) {
                string items = string.Join("|", o.Produdcts.Select(ci => $"{ci.Sku}:{ci.Quantity}"));
                sw.WriteLine($"{o.CustomerId},{o.OrderDate:O},{CsvEscape(items)}");
            }
            Console.WriteLine($"Wrote: {path}");
            return Task.CompletedTask;
        }

        public Task WriteNewRandomCustomersCsvAsync(List<Customer> customers, bool includePiiColumns) {
            var path = Path.Combine(_dir, $"customers_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            using var sw = new StreamWriter(path, false, Encoding.UTF8);

            // Keep columns aligned with your LoadColumn order.
            sw.WriteLine(string.Join(",", new[] {
            "CustomerId","Email","PhoneNumber","Churned","State","AreaCode","AccountLength","DaysSinceLastPurchase",
            "NumberOfMessages","TotalDaytimeMinutes","TotalDaytimeCalls","TotalDaytimeCharges",
            "TotalEveningMinutes","TotalEveningCalls","TotalEveningCharges",
            "TotalNightMinutes","TotalNightCalls","TotalNightCharges",
            "TotalInternationalMinutes","TotalInternationalCalls","TotalInternationalCharges",
            "NumberOfCustomerServiceCalls","InternationalPlan","Voice"
        }));

            foreach (Customer c in customers) {
                string? email = includePiiColumns ? c.Email : null;
                string? phone = includePiiColumns ? c.PhoneNumber : null;

                sw.WriteLine(string.Join(",", new[] {
                c.CustomerId.ToString(CultureInfo.InvariantCulture),
                CsvEscape(email),
                CsvEscape(phone),
                c.Churned ? "true" : "false",
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
                c.InternationalPlan ? "true" : "false",
                c.Voice ? "true" : "false",
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

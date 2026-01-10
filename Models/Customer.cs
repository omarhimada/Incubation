using Microsoft.ML.Data;

namespace Incubation.Models {
    public class Customer {
        [LoadColumn(0)]
        public float CustomerId { get; set; }

        [LoadColumn(1)]
        public string? Email { get; set; }

        [LoadColumn(2)]
        public string? PhoneNumber { get; set; }

        // Label
        [LoadColumn(3)]
        public bool Churned { get; set; }

        [LoadColumn(4)]
        public string? State { get; set; }

        [LoadColumn(5)]
        public string? AreaCode { get; set; }

        [LoadColumn(6)]
        public float AccountLength { get; set; }

        [LoadColumn(7)]
        public float DaysSinceLastPurchase { get; set; }

        [LoadColumn(8)]
        public float NumberOfMessages { get; set; }

        [LoadColumn(9)]
        public float TotalDaytimeMinutes { get; set; }

        [LoadColumn(10)]
        public float TotalDaytimeCalls { get; set; }

        [LoadColumn(11)]
        public float TotalDaytimeCharges { get; set; }

        [LoadColumn(12)]
        public float TotalEveningMinutes { get; set; }

        [LoadColumn(13)]
        public float TotalEveningCalls { get; set; }

        [LoadColumn(14)]
        public float TotalEveningCharges { get; set; }

        [LoadColumn(15)]
        public float TotalNightMinutes { get; set; }

        [LoadColumn(16)]
        public float TotalNightCalls { get; set; }

        [LoadColumn(17)]
        public float TotalNightCharges { get; set; }

        [LoadColumn(18)]
        public float TotalInternationalMinutes { get; set; }

        [LoadColumn(19)]
        public float TotalInternationalCalls { get; set; }

        [LoadColumn(20)]
        public float TotalInternationalCharges { get; set; }

        [LoadColumn(21)]
        public float NumberOfCustomerServiceCalls { get; set; }

        [LoadColumn(22)]
        public bool InternationalPlan { get; set; }

        [LoadColumn(23)]
        public bool Voice { get; set; }
    }

    public class ManyRandomCustomers {
        public List<Customer> Customers { get; set; } = new();
    }
}

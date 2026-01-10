using Microsoft.ML.Data;

namespace Incubation.Models {
    public sealed class Order {
        [LoadColumn(0)]
        public int CustomerId { get; set; }

        [LoadColumn(1)]
        public int Sku { get; set; }

        [LoadColumn(2)]
        public int Quantity { get; set; }

        [LoadColumn(3)]
        public float RecencyDays { get; set; }


        public const float TauDays = 60f;

        // Optional for building your estimator chain
        public static float QuantitySignal(float qty)
            => (float)Math.Log(1.0 + Math.Max(0f, qty));

        // Optional for building your estimator chain
        public static float RecencyWeightFromDays(float recencyDays)
            => (float)Math.Exp(-Math.Max(0f, recencyDays) / TauDays);
    }
}

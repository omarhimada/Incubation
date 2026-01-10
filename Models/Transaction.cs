namespace Incubation.Models {
    public sealed class Transaction {
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public List<Order> Produdcts { get; set; } = new();
    }

    public sealed class ManyRandomTransactions {
        public List<Transaction> Transactions { get; set; } = new();
    }
}
namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Wallet
{
    public class WalletDTO
    {
        public int WalletId { get; set; }
        public int UserId { get; set; }
        public float Balance { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WalletTransactionDTO
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public float Amount { get; set; }
        public string Type { get; set; } = string.Empty;       // CREDIT / DEBIT
        public string Source { get; set; } = string.Empty;     // PAYMENT, REFUND, etc.
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AddMoneyRequestDTO
    {
        public float Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // upi, card, netbanking
    }

    public class WalletPaymentRequestDTO
    {
        public int EventId { get; set; }
    }
}

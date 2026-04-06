namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public enum WalletTransactionType { CREDIT = 1, DEBIT = 2 }
    public enum WalletTransactionSource
    {
        PAYMENT = 1,
        REFUND = 2,
        COMMISSION = 3,
        COMPENSATION = 4,
        PENALTY = 5,
        ADD_MONEY = 6,
        ORGANIZER_EARNING = 7
    }

    public class WalletTransaction
    {
        public int TransactionId { get; set; }
        public int WalletId { get; set; }
        public Wallet? Wallet { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public float Amount { get; set; }
        public WalletTransactionType Type { get; set; }
        public WalletTransactionSource Source { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

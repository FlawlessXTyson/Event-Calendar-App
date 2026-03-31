namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class Wallet
    {
        public int WalletId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public float Balance { get; set; } = 0f;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

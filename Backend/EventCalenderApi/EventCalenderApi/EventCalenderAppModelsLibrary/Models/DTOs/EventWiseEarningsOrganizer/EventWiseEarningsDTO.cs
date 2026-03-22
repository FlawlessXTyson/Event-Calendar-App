namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment
{
    public class EventWiseEarningsDTO
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;

        public float TotalRevenue { get; set; }
        public float TotalCommission { get; set; }
        public float NetEarnings { get; set; }

        public int TotalTransactions { get; set; }
    }
}
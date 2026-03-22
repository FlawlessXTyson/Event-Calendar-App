namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment
{
    public class OrganizerEarningsDTO
    {
        public float TotalRevenue { get; set; }
        public float TotalCommission { get; set; }
        public float NetEarnings { get; set; }
        public int TotalTransactions { get; set; }
    }
}
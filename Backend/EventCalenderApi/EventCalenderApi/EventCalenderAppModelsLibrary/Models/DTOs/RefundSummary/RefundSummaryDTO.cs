namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event
{
    public class RefundSummaryDTO
    {
        public int EventId { get; set; }

        public int TotalUsersRefunded { get; set; }

        public float TotalRefundAmount { get; set; }
    }
}
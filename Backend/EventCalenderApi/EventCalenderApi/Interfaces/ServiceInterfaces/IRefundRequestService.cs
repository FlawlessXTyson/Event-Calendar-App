using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.RefundRequest;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IRefundRequestService
    {
        Task<RefundRequestResponseDTO> CreateAsync(int userId, int paymentId);
        Task<IEnumerable<RefundRequestResponseDTO>> GetPendingAsync();
        Task<RefundRequestResponseDTO> ApproveAsync(int refundRequestId, int adminId, float percentage);
        Task<RefundRequestResponseDTO> RejectAsync(int refundRequestId, int adminId);
    }
}

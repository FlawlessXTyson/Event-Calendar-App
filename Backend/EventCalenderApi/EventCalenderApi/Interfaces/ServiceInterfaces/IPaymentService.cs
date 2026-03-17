using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Commission;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponseDTO> CreatePaymentAsync(int userId, PaymentRequestDTO request);

        Task<IEnumerable<PaymentResponseDTO>> GetByUserAsync(int userId);

        Task<IEnumerable<PaymentResponseDTO>> GetByEventAsync(int eventId);

        Task<PaymentResponseDTO?> RefundAsync(int paymentId);

        Task<CommissionSummaryDTO> GetCommissionSummaryAsync();
    }
}
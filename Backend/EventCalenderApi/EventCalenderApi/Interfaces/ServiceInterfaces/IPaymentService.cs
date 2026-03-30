using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Commission;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponseDTO> CreatePaymentAsync(int userId, PaymentRequestDTO request);

        Task<IEnumerable<PaymentResponseDTO>> GetByUserAsync(int userId);

        Task<IEnumerable<PaymentResponseDTO>> GetByEventAsync(int eventId);

        Task<IEnumerable<PaymentResponseDTO>> GetAllPaymentsAsync(); // ADMIN

        Task<PagedResultDTO<PaymentResponseDTO>> GetOrganizerRefundsPagedAsync(int organizerId, int pageNumber, int pageSize);

        Task<PaymentResponseDTO> RefundAsync(int paymentId);

        Task<CommissionSummaryDTO> GetCommissionSummaryAsync();

        Task<OrganizerEarningsDTO> GetOrganizerEarningsAsync(int organizerId);
        Task<IEnumerable<EventWiseEarningsDTO>> GetEventWiseEarningsAsync(int organizerId);
    }
}
using Microsoft.EntityFrameworkCore;
using EventCalenderApi.EventCalenderAppDataLibrary;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Commission;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Exceptions;

namespace EventCalenderApi.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly EventCalendarDbContext _context;

        public PaymentService(EventCalendarDbContext context)
        {
            _context = context;
        }

        // ✅ CREATE PAYMENT
        public async Task<PaymentResponseDTO> CreatePaymentAsync(int userId, PaymentRequestDTO request)
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == request.EventId)
                ?? throw new NotFoundException("Event not found");

            if (!eventEntity.IsPaidEvent)
                throw new BadRequestException("This event does not require payment");

            if (eventEntity.Status != EventStatus.ACTIVE)
                throw new BadRequestException("Event is not active");

            if (eventEntity.ApprovalStatus != ApprovalStatus.APPROVED)
                throw new BadRequestException("Event is not approved");

            var alreadyPaid = await _context.Payments
                .AnyAsync(p =>
                    p.UserId == userId &&
                    p.EventId == request.EventId &&
                    p.Status == PaymentStatus.SUCCESS);

            if (alreadyPaid)
                throw new BadRequestException("You already paid for this event");

            float price = eventEntity.TicketPrice;
            float commission = price * eventEntity.CommissionPercentage / 100;
            float organizerAmount = price - commission;

            var payment = new Payment
            {
                UserId = userId,
                EventId = request.EventId,
                AmountPaid = price,
                CommissionAmount = commission,
                OrganizerAmount = organizerAmount,
                Status = PaymentStatus.SUCCESS,
                PaymentDate = DateTime.UtcNow
            };

            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();

            return MapToDTO(payment);
        }

        // ✅ GET USER PAYMENTS
        public async Task<IEnumerable<PaymentResponseDTO>> GetByUserAsync(int userId)
        {
            var payments = await _context.Payments
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return payments.Select(MapToDTO);
        }

        // ✅ GET EVENT PAYMENTS
        public async Task<IEnumerable<PaymentResponseDTO>> GetByEventAsync(int eventId)
        {
            var payments = await _context.Payments
                .Where(p => p.EventId == eventId)
                .ToListAsync();

            return payments.Select(MapToDTO);
        }

        // 🔥 FINAL REFUND LOGIC
        public async Task<PaymentResponseDTO> RefundAsync(int paymentId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId)
                ?? throw new NotFoundException("Payment not found");

            if (payment.Status == PaymentStatus.REFUNDED)
                throw new BadRequestException("Payment already refunded");

            payment.Status = PaymentStatus.REFUNDED;

            // ✅ TRACK REFUND
            payment.RefundedAmount = payment.AmountPaid;
            payment.RefundedAt = DateTime.UtcNow;

            // ✅ RESET BUSINESS VALUES
            payment.CommissionAmount = 0;
            payment.OrganizerAmount = 0;

            await _context.SaveChangesAsync();

            return MapToDTO(payment);
        }

        // ✅ ADMIN SUMMARY
        public async Task<CommissionSummaryDTO> GetCommissionSummaryAsync()
        {
            var successfulPayments = _context.Payments
                .Where(p => p.Status == PaymentStatus.SUCCESS);

            return new CommissionSummaryDTO
            {
                TotalCommission = await successfulPayments.SumAsync(p => p.CommissionAmount),
                TotalOrganizerPayout = await successfulPayments.SumAsync(p => p.OrganizerAmount),
                TotalPayments = await successfulPayments.CountAsync()
            };
        }

        private static PaymentResponseDTO MapToDTO(Payment p)
        {
            return new PaymentResponseDTO
            {
                PaymentId = p.PaymentId,
                EventId = p.EventId,
                AmountPaid = p.AmountPaid,
                RefundedAmount = p.RefundedAmount,
                Status = p.Status,
                PaymentDate = p.PaymentDate,
                RefundedAt = p.RefundedAt
            };
        }
    }
}
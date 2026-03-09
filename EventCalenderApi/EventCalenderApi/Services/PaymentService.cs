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

        // =========================================
        // CREATE PAYMENT
        // =========================================
        public async Task<PaymentResponseDTO> CreatePaymentAsync(int userId, PaymentRequestDTO request)
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == request.EventId);

            if (eventEntity == null)
                throw new NotFoundException("Event not found.");

            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.UserId == userId &&
                    p.EventId == request.EventId &&
                    p.Status == PaymentStatus.SUCCESS);

            if (existingPayment != null)
                throw new BadRequestException("You already paid for this event.");

            float price = eventEntity.TicketPrice;

            float commission = price / 10;

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

            var registration = new EventRegistration
            {
                UserId = userId,
                EventId = request.EventId,
                RegisteredAt = DateTime.UtcNow
            };

            await _context.EventRegistrations.AddAsync(registration);

            await _context.SaveChangesAsync();

            return new PaymentResponseDTO
            {
                PaymentId = payment.PaymentId,
                EventId = payment.EventId,
                AmountPaid = payment.AmountPaid,
                Status = payment.Status,
                PaymentDate = payment.PaymentDate
            };
        }

        // =========================================
        // GET PAYMENTS BY USER
        // =========================================
        public async Task<IEnumerable<PaymentResponseDTO>> GetByUserAsync(int userId)
        {
            return await _context.Payments
                .Where(p => p.UserId == userId)
                .Select(p => new PaymentResponseDTO
                {
                    PaymentId = p.PaymentId,
                    EventId = p.EventId,
                    AmountPaid = p.AmountPaid,
                    Status = p.Status,
                    PaymentDate = p.PaymentDate
                })
                .ToListAsync();
        }

        // =========================================
        // GET PAYMENTS BY EVENT
        // =========================================
        public async Task<IEnumerable<PaymentResponseDTO>> GetByEventAsync(int eventId)
        {
            return await _context.Payments
                .Where(p => p.EventId == eventId)
                .Select(p => new PaymentResponseDTO
                {
                    PaymentId = p.PaymentId,
                    EventId = p.EventId,
                    AmountPaid = p.AmountPaid,
                    Status = p.Status,
                    PaymentDate = p.PaymentDate
                })
                .ToListAsync();
        }

        // =========================================
        // REFUND
        // =========================================
        public async Task<PaymentResponseDTO?> RefundAsync(int paymentId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
                throw new NotFoundException("Payment not found.");

            if (payment.Status == PaymentStatus.REFUNDED)
                throw new BadRequestException("Payment already refunded.");

            payment.Status = PaymentStatus.REFUNDED;

            await _context.SaveChangesAsync();

            return new PaymentResponseDTO
            {
                PaymentId = payment.PaymentId,
                EventId = payment.EventId,
                AmountPaid = payment.AmountPaid,
                Status = payment.Status,
                PaymentDate = payment.PaymentDate
            };
        }

        // =========================================
        // COMMISSION SUMMARY
        // =========================================
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
    }
}
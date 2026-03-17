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


        //create payment
        public async Task<PaymentResponseDTO> CreatePaymentAsync(int userId, PaymentRequestDTO request)
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == request.EventId);

            if (eventEntity == null)
                throw new NotFoundException("Event not found.");

            if (!eventEntity.IsPaidEvent)
                throw new BadRequestException("This event does not require payment.");

            //duplicate payment check
            var alreadyPaid = await _context.Payments
                .AnyAsync(p =>
                    p.UserId == userId &&
                    p.EventId == request.EventId &&
                    p.Status == PaymentStatus.SUCCESS);

            if (alreadyPaid)
                throw new BadRequestException("You already paid for this event.");

            float price = eventEntity.TicketPrice;

            //commission calculation
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

            //create event registration automatically after payment
            var registration = new EventRegistration
            {
                UserId = userId,
                EventId = request.EventId,
                RegisteredAt = DateTime.UtcNow,
                Status = RegistrationStatus.REGISTERED
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


        //get payments by user
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


        //get payments by event
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


        //refund payment
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


        //commission summary
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
using EventCalenderApi.EventCalenderAppDataLibrary;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Commission;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.Enums;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly EventCalendarDbContext _context;
        private readonly IAuditLogRepository _auditRepo;

        public PaymentService(EventCalendarDbContext context, IAuditLogRepository auditRepo)
        {
            _context = context;
            _auditRepo = auditRepo;
        }

        public async Task<PaymentResponseDTO> CreatePaymentAsync(int userId, PaymentRequestDTO request)
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == request.EventId)
                ?? throw new NotFoundException("Event not found");

            // MUST REGISTER FIRST
            var isRegistered = await _context.EventRegistrations
                .AnyAsync(r =>
                    r.UserId == userId &&
                    r.EventId == request.EventId &&
                    r.Status == RegistrationStatus.REGISTERED);

            if (!isRegistered)
                throw new BadRequestException("You must register before making payment");

            if (!eventEntity.IsPaidEvent)
                throw new BadRequestException("This event does not require payment");

            if (eventEntity.Status != EventStatus.ACTIVE)
                throw new BadRequestException("Event is not active");

            if (eventEntity.ApprovalStatus != ApprovalStatus.APPROVED)
                throw new BadRequestException("Event is not approved");

            // ================= TIME VALIDATION =================
            var now = DateTime.UtcNow;

            //  START CHECK
            var eventStartDateTime = eventEntity.EventDate.Add(eventEntity.StartTime ?? TimeSpan.Zero);

            if (now >= eventStartDateTime)
                throw new BadRequestException("Event already started");

            //  END CHECK
            var eventEndDate = eventEntity.EventEndDate ?? eventEntity.EventDate;
            var eventEndTime = eventEntity.EndTime ?? new TimeSpan(23, 59, 59);

            var eventEndDateTime = eventEndDate.Add(eventEndTime);

            if (now > eventEndDateTime)
                throw new BadRequestException("Event already ended");

            // ================= SEAT CHECK =================
            if (eventEntity.SeatsLimit != null)
            {
                var totalPaid = await _context.Payments
                    .CountAsync(p =>
                        p.EventId == request.EventId &&
                        p.Status == PaymentStatus.SUCCESS);

                if (totalPaid >= eventEntity.SeatsLimit)
                    throw new BadRequestException("Event is fully booked");
            }

            // ================= DUPLICATE =================
            var alreadyPaid = await _context.Payments
                .AnyAsync(p =>
                    p.UserId == userId &&
                    p.EventId == request.EventId &&
                    p.Status == PaymentStatus.SUCCESS);

            if (alreadyPaid)
                throw new BadRequestException("You already paid for this event");

            // ================= CALC =================
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

            // ================= AUDIT =================
            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Role = "USER",
                Action = "PAYMENT_SUCCESS",
                Entity = "Payment",
                EntityId = payment.PaymentId
            });

            return MapToDTO(payment);
        }

        public async Task<IEnumerable<PaymentResponseDTO>> GetByUserAsync(int userId)
        {
            var payments = await _context.Payments
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToDTO);
        }

        public async Task<IEnumerable<PaymentResponseDTO>> GetByEventAsync(int eventId)
        {
            var payments = await _context.Payments
                .Where(p => p.EventId == eventId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToDTO);
        }

        public async Task<IEnumerable<PaymentResponseDTO>> GetAllPaymentsAsync()
        {
            var payments = await _context.Payments
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToDTO);
        }

        public async Task<PaymentResponseDTO> RefundAsync(int paymentId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId)
                ?? throw new NotFoundException("Payment not found");

            if (payment.Status == PaymentStatus.REFUNDED)
                throw new BadRequestException("Payment already refunded");

            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == payment.EventId);

            if (eventEntity != null && eventEntity.EventDate < DateTime.UtcNow)
                throw new BadRequestException("Cannot refund after event has started");

            payment.Status = PaymentStatus.REFUNDED;
            payment.RefundedAmount = payment.AmountPaid;
            payment.RefundedAt = DateTime.UtcNow;

            payment.CommissionAmount = 0;
            payment.OrganizerAmount = 0;

            await _context.SaveChangesAsync();

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = payment.UserId,
                Role = "USER",
                Action = "REFUND",
                Entity = "Payment",
                EntityId = payment.PaymentId
            });

            return MapToDTO(payment);
        }

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
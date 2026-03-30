using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Commission;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.Enums;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Helpers;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IRepository<int, Event> _eventRepo;
        private readonly IRepository<int, EventRegistration> _registrationRepo;
        private readonly IRepository<int, Payment> _paymentRepo;
        private readonly IAuditLogRepository _auditRepo;

        public PaymentService(
            IRepository<int, Event> eventRepo,
            IRepository<int, EventRegistration> registrationRepo,
            IRepository<int, Payment> paymentRepo,
            IAuditLogRepository auditRepo)
        {
            _eventRepo = eventRepo;
            _registrationRepo = registrationRepo;
            _paymentRepo = paymentRepo;
            _auditRepo = auditRepo;
        }

        // ================= CREATE PAYMENT =================
        public async Task<PaymentResponseDTO> CreatePaymentAsync(int userId, PaymentRequestDTO request)
        {
            var eventEntity = await _eventRepo.GetByIdAsync(request.EventId)
                ?? throw new NotFoundException("Event not found");

            var isRegistered = await _registrationRepo.GetQueryable()
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
            var now = IstClock.Now; // IST — EventDate is stored in IST

            var eventStartDateTime = eventEntity.EventDate.Add(eventEntity.StartTime ?? TimeSpan.Zero);

            if (now >= eventStartDateTime)
                throw new BadRequestException("Event already started");

            var eventEndDate = eventEntity.EventEndDate ?? eventEntity.EventDate;
            var eventEndTime = eventEntity.EndTime ?? new TimeSpan(23, 59, 59);
            var eventEndDateTime = eventEndDate.Add(eventEndTime);

            if (now > eventEndDateTime)
                throw new BadRequestException("Event already ended");

            // ================= SEAT CHECK =================
            // Count only payments linked to currently-active registrations
            if (eventEntity.SeatsLimit != null)
            {
                var totalPaid = await _paymentRepo.GetQueryable()
                    .CountAsync(p =>
                        p.EventId == request.EventId &&
                        p.Status == PaymentStatus.SUCCESS &&
                        _registrationRepo.GetQueryable().Any(r =>
                            r.UserId == p.UserId &&
                            r.EventId == request.EventId &&
                            r.Status == RegistrationStatus.REGISTERED));

                if (totalPaid >= eventEntity.SeatsLimit)
                    throw new BadRequestException("Event is fully booked");
            }

            // ================= DUPLICATE =================
            // Only block if user is CURRENTLY REGISTERED and has a SUCCESS payment.
            // A cancelled registration's payment stays SUCCESS until admin processes the refund,
            // so we must check active registration status to avoid false "already paid" errors.
            //var alreadyPaid = await _paymentRepo.GetQueryable()
            //    .AnyAsync(p =>
            //        p.UserId == userId &&
            //        p.EventId == request.EventId &&
            //        p.Status == PaymentStatus.SUCCESS &&
            //        _registrationRepo.GetQueryable().Any(r =>
            //            r.UserId == userId &&
            //            r.EventId == request.EventId &&
            //            r.Status == RegistrationStatus.REGISTERED));

            //if (alreadyPaid)
            //    throw new BadRequestException("You already paid for this event");

            // ================= DUPLICATE =================
            // FIX: Handle refund flow correctly without breaking existing logic
            var existingPayment = await _paymentRepo.GetQueryable()
                .Where(p =>
                    p.UserId == userId &&
                    p.EventId == request.EventId)
                .OrderByDescending(p => p.PaymentDate)
                .FirstOrDefaultAsync();

            if (existingPayment != null)
            {
                // Case 1: Already paid and still registered
                var isStillRegistered = await _registrationRepo.GetQueryable()
                    .AnyAsync(r =>
                        r.UserId == userId &&
                        r.EventId == request.EventId &&
                        r.Status == RegistrationStatus.REGISTERED);

                if (existingPayment.Status == PaymentStatus.SUCCESS && isStillRegistered)
                    throw new BadRequestException("You already paid for this event");

                // Case 2: Refund not completed yet
                if (existingPayment.Status != PaymentStatus.REFUNDED)
                    throw new BadRequestException("Please wait for refund to complete");

                // Case 3: Refunded → allow again
            }

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

            var created = await _paymentRepo.AddAsync(payment);

            // ================= AUDIT =================
            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Role = "USER",
                Action = "PAYMENT_SUCCESS",
                Entity = "Payment",
                EntityId = created.PaymentId
            });

            return MapToDTO(created);
        }

        // ================= USER PAYMENTS =================
        public async Task<IEnumerable<PaymentResponseDTO>> GetByUserAsync(int userId)
        {
            var payments = await _paymentRepo.GetQueryable()
                .Include(p => p.Event)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToDTO);
        }

        // ================= EVENT PAYMENTS =================
        public async Task<IEnumerable<PaymentResponseDTO>> GetByEventAsync(int eventId)
        {
            var payments = await _paymentRepo.GetQueryable()
                .Include(p => p.Event)
                .Include(p => p.User)
                .Where(p => p.EventId == eventId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToDTO);
        }

        // ================= ADMIN =================
        public async Task<IEnumerable<PaymentResponseDTO>> GetAllPaymentsAsync()
        {
            var payments = await _paymentRepo.GetQueryable()
                .Include(p => p.Event)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToDTO);
        }

        // ================= REFUND =================
        public async Task<PaymentResponseDTO> RefundAsync(int paymentId)
        {
            var payment = await _paymentRepo.GetByIdAsync(paymentId)
                ?? throw new NotFoundException("Payment not found");

            if (payment.Status == PaymentStatus.REFUNDED)
                throw new BadRequestException("Payment already refunded");

            //var eventEntity = await _eventRepo.GetByIdAsync(payment.EventId);
            var eventEntity = await _eventRepo.GetByIdAsync(payment.EventId)
                ?? throw new NotFoundException("Event not found");

            //if (eventEntity != null && eventEntity.EventDate < DateTime.UtcNow)
            //    throw new BadRequestException("Cannot refund after event has started");
            var eventEndDate = eventEntity.EventEndDate ?? eventEntity.EventDate;
            var eventEndTime = eventEntity.EndTime ?? new TimeSpan(23, 59, 59);
            var eventEndDateTime = eventEndDate.Add(eventEndTime);

            if (IstClock.Now > eventEndDateTime)
                throw new BadRequestException("Cannot refund after event has ended");

            payment.Status = PaymentStatus.REFUNDED;
            payment.RefundedAmount = payment.AmountPaid;
            payment.RefundedAt = DateTime.UtcNow;
            payment.CommissionAmount = 0;
            payment.OrganizerAmount = 0;

            var updated = await _paymentRepo.UpdateAsync(payment.PaymentId, payment);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = payment.UserId,
                Role = "USER",
                Action = "REFUND",
                Entity = "Payment",
                EntityId = payment.PaymentId
            });

            return MapToDTO(updated!);
        }

        // ================= COMMISSION =================
        public async Task<CommissionSummaryDTO> GetCommissionSummaryAsync()
        {
            var successfulPayments = _paymentRepo.GetQueryable()
                .Where(p => p.Status == PaymentStatus.SUCCESS);

            return new CommissionSummaryDTO
            {
                TotalCommission = await successfulPayments.SumAsync(p => p.CommissionAmount),
                TotalOrganizerPayout = await successfulPayments.SumAsync(p => p.OrganizerAmount),
                TotalPayments = await successfulPayments.CountAsync()
            };
        }

        // ================= ORGANIZER TOTAL =================
        public async Task<OrganizerEarningsDTO> GetOrganizerEarningsAsync(int organizerId)
        {
            var payments = await _paymentRepo.GetQueryable()
                .Include(p => p.Event)
                .Where(p =>
                    p.Status == PaymentStatus.SUCCESS &&
                    p.Event != null &&
                    p.Event.CreatedByUserId == organizerId)
                .ToListAsync();

            return new OrganizerEarningsDTO
            {
                TotalRevenue = payments.Sum(p => p.AmountPaid),
                TotalCommission = payments.Sum(p => p.CommissionAmount),
                NetEarnings = payments.Sum(p => p.OrganizerAmount),
                TotalTransactions = payments.Count
            };
        }

        // ================= EVENT WISE =================
        public async Task<IEnumerable<EventWiseEarningsDTO>> GetEventWiseEarningsAsync(int organizerId)
        {
            var payments = await _paymentRepo.GetQueryable()
                .Include(p => p.Event)
                .Where(p =>
                    p.Status == PaymentStatus.SUCCESS &&
                    p.Event != null &&
                    p.Event.CreatedByUserId == organizerId)
                .ToListAsync();

            return payments
                .GroupBy(p => p.EventId)
                .Select(group => new EventWiseEarningsDTO
                {
                    EventId = group.Key,
                    EventTitle = group.First().Event!.Title,
                    TotalRevenue = group.Sum(p => p.AmountPaid),
                    TotalCommission = group.Sum(p => p.CommissionAmount),
                    NetEarnings = group.Sum(p => p.OrganizerAmount),
                    TotalTransactions = group.Count()
                })
                .OrderByDescending(e => e.TotalRevenue)
                .ToList();
        }

        private static PaymentResponseDTO MapToDTO(Payment p)
        {
            return new PaymentResponseDTO
            {
                PaymentId = p.PaymentId,
                EventId = p.EventId,
                EventTitle = p.Event?.Title ?? string.Empty,
                UserId = p.UserId,
                UserName  = p.User?.Name  ?? string.Empty,
                UserEmail = p.User?.Email ?? string.Empty,
                AmountPaid = p.AmountPaid,
                RefundedAmount = p.RefundedAmount,
                Status = p.Status,
                PaymentDate = p.PaymentDate,
                RefundedAt = p.RefundedAt
            };
        }
    }
}
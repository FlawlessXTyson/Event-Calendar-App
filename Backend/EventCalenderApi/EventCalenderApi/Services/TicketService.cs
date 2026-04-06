using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Ticket;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class TicketService : ITicketService
    {
        private readonly IRepository<int, Ticket> _ticketRepo;
        private readonly IRepository<int, Event> _eventRepo;
        private readonly IRepository<int, User> _userRepo;
        private readonly IRepository<int, Payment> _paymentRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IEmailService _emailService;

        public TicketService(
            IRepository<int, Ticket> ticketRepo,
            IRepository<int, Event> eventRepo,
            IRepository<int, User> userRepo,
            IRepository<int, Payment> paymentRepo,
            IAuditLogRepository auditRepo,
            IEmailService emailService)
        {
            _ticketRepo = ticketRepo;
            _eventRepo = eventRepo;
            _userRepo = userRepo;
            _paymentRepo = paymentRepo;
            _auditRepo = auditRepo;
            _emailService = emailService;
        }

        public async Task<TicketResponseDTO> GenerateTicketAsync(int userId, int eventId, int? paymentId)
        {
            // Idempotent — return existing ticket if already generated
            var existing = await _ticketRepo.GetQueryable()
                .Include(t => t.User)
                .Include(t => t.Event)
                .Include(t => t.Payment)
                .FirstOrDefaultAsync(t => t.UserId == userId && t.EventId == eventId);

            if (existing != null)
                return MapToDTO(existing);

            var ev = await _eventRepo.GetByIdAsync(eventId)
                ?? throw new NotFoundException("Event not found");

            var user = await _userRepo.GetByIdAsync(userId)
                ?? throw new NotFoundException("User not found");

            var ticket = new Ticket
            {
                UserId = userId,
                EventId = eventId,
                PaymentId = paymentId,
                GeneratedAt = DateTime.UtcNow
            };

            var created = await _ticketRepo.AddAsync(ticket);

            // Reload with navigation properties
            var full = await _ticketRepo.GetQueryable()
                .Include(t => t.User)
                .Include(t => t.Event)
                .Include(t => t.Payment)
                .FirstAsync(t => t.TicketId == created.TicketId);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Role = "USER",
                Action = "TICKET_GENERATED",
                Entity = "Ticket",
                EntityId = created.TicketId
            });

            var dto = MapToDTO(full);

            // Send ticket email — fire-and-forget so SMTP issues never break the booking flow
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendTicketEmailAsync(user.Email, user.Name, dto);
                }
                catch
                {
                    // Email failure is non-critical; ticket is already saved
                }
            });

            return dto;
        }

        public async Task<TicketResponseDTO?> GetTicketAsync(int userId, int eventId)
        {
            var ticket = await _ticketRepo.GetQueryable()
                .Include(t => t.User)
                .Include(t => t.Event)
                .Include(t => t.Payment)
                .FirstOrDefaultAsync(t => t.UserId == userId && t.EventId == eventId);

            return ticket == null ? null : MapToDTO(ticket);
        }

        public async Task<IEnumerable<TicketResponseDTO>> GetMyTicketsAsync(int userId)
        {
            var tickets = await _ticketRepo.GetQueryable()
                .Include(t => t.User)
                .Include(t => t.Event)
                .Include(t => t.Payment)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.GeneratedAt)
                .ToListAsync();

            return tickets.Select(MapToDTO);
        }

        private static TicketResponseDTO MapToDTO(Ticket t)
        {
            return new TicketResponseDTO
            {
                TicketId = t.TicketId,
                UserId = t.UserId,
                UserName = t.User?.Name ?? string.Empty,
                EventId = t.EventId,
                EventTitle = t.Event?.Title ?? string.Empty,
                EventDescription = t.Event?.Description ?? string.Empty,
                EventLocation = t.Event?.Location ?? string.Empty,
                EventDate = t.Event?.EventDate ?? DateTime.MinValue,
                StartTime = t.Event?.StartTime?.ToString(@"hh\:mm"),
                EndTime = t.Event?.EndTime?.ToString(@"hh\:mm"),
                PaymentId = t.PaymentId,
                AmountPaid = t.Payment?.AmountPaid ?? 0,
                IsPaidEvent = t.Event?.IsPaidEvent ?? false,
                GeneratedAt = t.GeneratedAt
            };
        }
    }
}

using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly IEmailService _emailService;

        public TicketController(ITicketService ticketService, IEmailService emailService)
        {
            _ticketService = ticketService;
            _emailService = emailService;
        }

        // POST /api/Ticket/generate — generate ticket after registration/payment
        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateTicketRequest req)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _ticketService.GenerateTicketAsync(userId, req.EventId, req.PaymentId));
        }

        // GET /api/Ticket/event/{eventId} — get ticket for a specific event
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetByEvent(int eventId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var ticket = await _ticketService.GetTicketAsync(userId, eventId);
            if (ticket == null) return NotFound(new { message = "No ticket found for this event" });
            return Ok(ticket);
        }

        // GET /api/Ticket/my — all tickets for logged-in user
        [HttpGet("my")]
        public async Task<IActionResult> GetMyTickets()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _ticketService.GetMyTicketsAsync(userId));
        }

        // POST /api/Ticket/send-email — send ticket confirmation email with HTML rendered by frontend
        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmail([FromBody] SendTicketEmailRequest req)
        {
            try
            {
                await _emailService.SendEmailAsync(req.ToEmail, req.ToName, req.Subject, req.HtmlBody);
                return Ok(new { message = "Email sent successfully" });
            }
            catch
            {
                // Non-critical — ticket already exists, email failure should not surface as error
                return Ok(new { message = "Email delivery skipped" });
            }
        }
    }

    public class GenerateTicketRequest
    {
        public int EventId { get; set; }
        public int? PaymentId { get; set; }
    }

    public class SendTicketEmailRequest
    {
        public string ToEmail { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
    }
}

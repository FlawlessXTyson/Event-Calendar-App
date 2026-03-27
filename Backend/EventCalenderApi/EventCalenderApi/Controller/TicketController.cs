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

        public TicketController(ITicketService ticketService)
        {
            _ticketService = ticketService;
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
    }

    public class GenerateTicketRequest
    {
        public int EventId { get; set; }
        public int? PaymentId { get; set; }
    }
}

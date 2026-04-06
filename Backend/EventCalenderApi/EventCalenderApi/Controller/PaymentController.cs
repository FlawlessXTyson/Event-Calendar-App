using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _service;

        public PaymentController(IPaymentService service)
        {
            _service = service;
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> Create(PaymentRequestDTO request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _service.CreatePaymentAsync(userId, request));
        }

        // ================= USER HISTORY =================
        [HttpGet("my-payments")]
        public async Task<IActionResult> GetMyPayments()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _service.GetByUserAsync(userId));
        }

        // ================= EVENT PAYMENTS =================
        [Authorize(Roles = "ADMIN,ORGANIZER")]
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetByEvent(int eventId)
        {
            return Ok(await _service.GetByEventAsync(eventId));
        }

        // ================= ADMIN: ALL PAYMENTS =================
        [Authorize(Roles = "ADMIN")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllPayments()
        {
            return Ok(await _service.GetAllPaymentsAsync());
        }

        // ================= REFUND =================
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{paymentId}/refund")]
        public async Task<IActionResult> Refund(int paymentId)
        {
            return Ok(await _service.RefundAsync(paymentId));
        }

        // ================= COMMISSION =================
        [Authorize(Roles = "ADMIN")]
        [HttpGet("commission-summary")]
        public async Task<IActionResult> GetCommissionSummary()
        {
            return Ok(await _service.GetCommissionSummaryAsync());
        }

        // ================= ORGANIZER REFUNDS PAGED =================
        [Authorize(Roles = "ORGANIZER")]
        [HttpGet("organizer-refunds")]
        public async Task<IActionResult> GetOrganizerRefunds(int pageNumber = 1, int pageSize = 10)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _service.GetOrganizerRefundsPagedAsync(userId, pageNumber, pageSize));
        }

        // ================= ORGANIZER EARNINGS =================
        [Authorize(Roles = "ORGANIZER")]
        [HttpGet("organizer-earnings")]
        public async Task<IActionResult> GetOrganizerEarnings()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.GetOrganizerEarningsAsync(userId));
        }

        // ================= ORGANIZER EVENT-WISE EARNINGS =================
        [Authorize(Roles = "ORGANIZER")]
        [HttpGet("organizer-event-earnings")]
        public async Task<IActionResult> GetEventWiseEarnings()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.GetEventWiseEarningsAsync(userId));
        }
    }
}
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

        [HttpPost]
        public async Task<IActionResult> Create(PaymentRequestDTO request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _service.CreatePaymentAsync(userId, request));
        }

        [HttpGet("my-payments")]
        public async Task<IActionResult> GetMyPayments()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _service.GetByUserAsync(userId));
        }

        [Authorize(Roles = "ADMIN,ORGANIZER")]
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetByEvent(int eventId)
        {
            return Ok(await _service.GetByEventAsync(eventId));
        }

        // 🔥 ONLY ADMIN CAN FORCE REFUND
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{paymentId}/refund")]
        public async Task<IActionResult> Refund(int paymentId)
        {
            return Ok(await _service.RefundAsync(paymentId));
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("commission-summary")]
        public async Task<IActionResult> GetCommissionSummary()
        {
            return Ok(await _service.GetCommissionSummaryAsync());
        }
    }
}
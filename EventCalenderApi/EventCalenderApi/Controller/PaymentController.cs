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

        // =========================================
        // CREATE PAYMENT
        // =========================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentRequestDTO request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _service.CreatePaymentAsync(userId, request);

            return Ok(result);
        }

        // =========================================
        // GET MY PAYMENTS
        // =========================================
        [HttpGet("my-payments")]
        public async Task<IActionResult> GetMyPayments()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.GetByUserAsync(userId));
        }

        // =========================================
        // GET PAYMENTS BY EVENT
        // =========================================
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetByEvent(int eventId)
        {
            return Ok(await _service.GetByEventAsync(eventId));
        }

        // =========================================
        // REFUND
        // =========================================
        [HttpPut("{paymentId}/refund")]
        public async Task<IActionResult> Refund(int paymentId)
        {
            var result = await _service.RefundAsync(paymentId);

            if (result == null)
                return NotFound("Payment not found.");

            return Ok(result);
        }

        // =========================================
        // COMMISSION SUMMARY
        // =========================================
        [Authorize(Roles = "ADMIN")]
        [HttpGet("commission-summary")]
        public async Task<IActionResult> GetCommissionSummary()
        {
            return Ok(await _service.GetCommissionSummaryAsync());
        }
    }
}
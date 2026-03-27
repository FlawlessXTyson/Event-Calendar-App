using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.RefundRequest;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RefundRequestController : ControllerBase
    {
        private readonly IRefundRequestService _svc;
        public RefundRequestController(IRefundRequestService svc) => _svc = svc;

        // POST /api/RefundRequest — user requests refund for a payment
        [HttpPost("{paymentId}")]
        public async Task<IActionResult> Create(int paymentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _svc.CreateAsync(userId, paymentId));
        }

        // GET /api/RefundRequest/pending — admin sees all pending
        [Authorize(Roles = "ADMIN")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
            => Ok(await _svc.GetPendingAsync());

        // PUT /api/RefundRequest/{id}/approve — admin approves with percentage
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveRefundDTO dto)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _svc.ApproveAsync(id, adminId, dto.RefundPercentage));
        }

        // PUT /api/RefundRequest/{id}/reject — admin rejects
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _svc.RejectAsync(id, adminId));
        }
    }
}

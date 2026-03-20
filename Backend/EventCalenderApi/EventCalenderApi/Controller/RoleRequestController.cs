using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoleRequestController : ControllerBase
{
    private readonly IRoleRequestService _service;

    public RoleRequestController(IRoleRequestService service)
    {
        _service = service;
    }

    [HttpPost("request-organizer")]
    public async Task<IActionResult> RequestOrganizer()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        return Ok(await _service.RequestOrganizerRoleAsync(userId));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        return Ok(await _service.GetPendingRequestsAsync());
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        return Ok(await _service.ApproveRequestAsync(id, adminId));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        return Ok(await _service.RejectRequestAsync(id, adminId));
    }
}
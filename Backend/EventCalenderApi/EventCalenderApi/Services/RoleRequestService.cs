using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.Enums;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Exceptions;
using Microsoft.EntityFrameworkCore;

public class RoleRequestService : IRoleRequestService
{
    private readonly IRepository<int, RoleChangeRequest> _requestRepo;
    private readonly IRepository<int, User> _userRepo;

    public RoleRequestService(
        IRepository<int, RoleChangeRequest> requestRepo,
        IRepository<int, User> userRepo)
    {
        _requestRepo = requestRepo;
        _userRepo = userRepo;
    }

    // ✅ USER REQUEST ORGANIZER ROLE (FIXED)
    public async Task<string> RequestOrganizerRoleAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found");

        if (user.Role == UserRole.ORGANIZER)
            throw new BadRequestException("You are already an organizer");

        // 🔥 FIX: Prevent duplicate pending requests
        var existingRequest = await _requestRepo
            .GetQueryable()
            .Where(r => r.UserId == userId && r.Status == RequestStatus.PENDING)
            .FirstOrDefaultAsync();

        if (existingRequest != null)
            throw new BadRequestException("You already have a pending request");

        var request = new RoleChangeRequest
        {
            UserId = userId,
            RequestedRole = UserRole.ORGANIZER,
            Status = RequestStatus.PENDING,
            RequestedAt = DateTime.UtcNow
        };

        await _requestRepo.AddAsync(request);

        return "Request submitted successfully";
    }

    // ADMIN VIEW PENDING
    public async Task<IEnumerable<RoleChangeRequest>> GetPendingRequestsAsync()
    {
        return await _requestRepo
            .GetQueryable()
            .Where(r => r.Status == RequestStatus.PENDING)
            .ToListAsync();
    }

    // ADMIN APPROVE
    public async Task<string> ApproveRequestAsync(int requestId, int adminId)
    {
        var request = await _requestRepo.GetByIdAsync(requestId)
            ?? throw new NotFoundException("Request not found");

        if (request.Status != RequestStatus.PENDING)
            throw new BadRequestException("Request already processed");

        var user = await _userRepo.GetByIdAsync(request.UserId)
            ?? throw new NotFoundException("User not found");

        // update role
        user.Role = UserRole.ORGANIZER;
        await _userRepo.UpdateAsync(user.UserId, user);

        // update request
        request.Status = RequestStatus.APPROVED;
        request.ReviewedByAdminId = adminId;

        await _requestRepo.UpdateAsync(requestId, request);

        return "User promoted to organizer";
    }

    // ADMIN REJECT
    public async Task<string> RejectRequestAsync(int requestId, int adminId)
    {
        var request = await _requestRepo.GetByIdAsync(requestId)
            ?? throw new NotFoundException("Request not found");

        if (request.Status != RequestStatus.PENDING)
            throw new BadRequestException("Request already processed");

        request.Status = RequestStatus.REJECTED;
        request.ReviewedByAdminId = adminId;

        await _requestRepo.UpdateAsync(requestId, request);

        return "Request rejected";
    }
}
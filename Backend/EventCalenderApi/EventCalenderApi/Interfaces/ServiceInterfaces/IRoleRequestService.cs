using EventCalenderApi.EventCalenderAppModelsLibrary.Models;

public interface IRoleRequestService
{
    Task<string> RequestOrganizerRoleAsync(int userId);

    Task<IEnumerable<RoleChangeRequest>> GetPendingRequestsAsync();

    Task<string> ApproveRequestAsync(int requestId, int adminId);

    Task<string> RejectRequestAsync(int requestId, int adminId);
}
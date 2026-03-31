using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.Enums;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace EventCalenderApi.Tests.Services
{
    public class RoleRequestServiceTests
    {
        private readonly Mock<IRepository<int, RoleChangeRequest>> _requestRepoMock = new();
        private readonly Mock<IRepository<int, User>> _userRepoMock = new();
        private RoleRequestService CreateService() => new(_requestRepoMock.Object, _userRepoMock.Object);

        // ── RequestOrganizerRoleAsync ────────────────────────────────────

        [Fact]
        public async Task RequestOrganizerRole_Success_ReturnsMessage()
        {
            var user = new User { UserId = 1, Role = UserRole.USER };
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var pending = new List<RoleChangeRequest>();
            _requestRepoMock.Setup(r => r.GetQueryable()).Returns(pending.BuildMock());
            _requestRepoMock.Setup(r => r.AddAsync(It.IsAny<RoleChangeRequest>()))
                .ReturnsAsync(new RoleChangeRequest { RequestId = 1 });

            var result = await CreateService().RequestOrganizerRoleAsync(1);
            Assert.Equal("Request submitted successfully", result);
        }

        [Fact]
        public async Task RequestOrganizerRole_UserNotFound_ThrowsNotFound()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().RequestOrganizerRoleAsync(99));
        }

        [Fact]
        public async Task RequestOrganizerRole_AlreadyOrganizer_ThrowsBadRequest()
        {
            var user = new User { UserId = 1, Role = UserRole.ORGANIZER };
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().RequestOrganizerRoleAsync(1));
        }

        [Fact]
        public async Task RequestOrganizerRole_PendingExists_ThrowsBadRequest()
        {
            var user = new User { UserId = 1, Role = UserRole.USER };
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var pending = new List<RoleChangeRequest>
            {
                new() { RequestId = 1, UserId = 1, Status = RequestStatus.PENDING }
            };
            _requestRepoMock.Setup(r => r.GetQueryable()).Returns(pending.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().RequestOrganizerRoleAsync(1));
        }

        // ── GetPendingRequestsAsync ──────────────────────────────────────

        [Fact]
        public async Task GetPendingRequests_ReturnsPendingOnly()
        {
            var requests = new List<RoleChangeRequest>
            {
                new() { RequestId = 1, UserId = 1, Status = RequestStatus.PENDING },
                new() { RequestId = 2, UserId = 2, Status = RequestStatus.APPROVED }
            };
            _requestRepoMock.Setup(r => r.GetQueryable()).Returns(requests.BuildMock());

            var result = (await CreateService().GetPendingRequestsAsync()).ToList();
            Assert.Single(result);
            Assert.Equal(RequestStatus.PENDING, result[0].Status);
        }

        // ── ApproveRequestAsync ──────────────────────────────────────────

        [Fact]
        public async Task ApproveRequest_Success_PromotesUser()
        {
            var request = new RoleChangeRequest { RequestId = 1, UserId = 2, Status = RequestStatus.PENDING };
            var user = new User { UserId = 2, Role = UserRole.USER };

            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.UpdateAsync(2, user)).ReturnsAsync(user);
            _requestRepoMock.Setup(r => r.UpdateAsync(1, request)).ReturnsAsync(request);

            var result = await CreateService().ApproveRequestAsync(1, 99);
            Assert.Equal("User promoted to organizer", result);
            Assert.Equal(UserRole.ORGANIZER, user.Role);
        }

        [Fact]
        public async Task ApproveRequest_NotFound_ThrowsNotFound()
        {
            _requestRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RoleChangeRequest?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().ApproveRequestAsync(99, 1));
        }

        [Fact]
        public async Task ApproveRequest_AlreadyProcessed_ThrowsBadRequest()
        {
            var request = new RoleChangeRequest { RequestId = 1, Status = RequestStatus.APPROVED };
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().ApproveRequestAsync(1, 1));
        }

        [Fact]
        public async Task ApproveRequest_UserNotFound_ThrowsNotFound()
        {
            var request = new RoleChangeRequest { RequestId = 1, UserId = 99, Status = RequestStatus.PENDING };
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().ApproveRequestAsync(1, 1));
        }

        // ── RejectRequestAsync ───────────────────────────────────────────

        [Fact]
        public async Task RejectRequest_Success_ReturnsMessage()
        {
            var request = new RoleChangeRequest { RequestId = 1, Status = RequestStatus.PENDING };
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            _requestRepoMock.Setup(r => r.UpdateAsync(1, request)).ReturnsAsync(request);

            var result = await CreateService().RejectRequestAsync(1, 99);
            Assert.Equal("Request rejected", result);
            Assert.Equal(RequestStatus.REJECTED, request.Status);
        }

        [Fact]
        public async Task RejectRequest_NotFound_ThrowsNotFound()
        {
            _requestRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RoleChangeRequest?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().RejectRequestAsync(99, 1));
        }

        [Fact]
        public async Task RejectRequest_AlreadyProcessed_ThrowsBadRequest()
        {
            var request = new RoleChangeRequest { RequestId = 1, Status = RequestStatus.REJECTED };
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().RejectRequestAsync(1, 1));
        }
    }
}






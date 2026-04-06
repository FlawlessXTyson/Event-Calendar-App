using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.Enums;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Services;

public class RoleRequestServiceTests
{
    private readonly Mock<IRepository<int, RoleChangeRequest>> _requestRepoMock = new();
    private readonly Mock<IRepository<int, User>> _userRepoMock = new();
    private readonly RoleRequestService _sut;

    public RoleRequestServiceTests()
    {
        _sut = new RoleRequestService(_requestRepoMock.Object, _userRepoMock.Object);
    }

    // ── RequestOrganizerRoleAsync ──────────────────────────────────────────

    [Fact]
    public async Task RequestOrganizerRoleAsync_Should_ReturnSuccess_When_ValidUser()
    {
        // Arrange
        var user = new User { UserId = 1, Role = UserRole.USER };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _requestRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RoleChangeRequest>().BuildMock());
        _requestRepoMock.Setup(r => r.AddAsync(It.IsAny<RoleChangeRequest>()))
            .ReturnsAsync(new RoleChangeRequest { RequestId = 1 });

        // Act
        var result = await _sut.RequestOrganizerRoleAsync(1);

        // Assert
        Assert.Equal("Request submitted successfully", result);
    }

    [Fact]
    public async Task RequestOrganizerRoleAsync_Should_ThrowNotFound_When_UserDoesNotExist()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RequestOrganizerRoleAsync(99));
    }

    [Fact]
    public async Task RequestOrganizerRoleAsync_Should_ThrowBadRequest_When_AlreadyOrganizer()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { UserId = 1, Role = UserRole.ORGANIZER });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RequestOrganizerRoleAsync(1));
    }

    [Fact]
    public async Task RequestOrganizerRoleAsync_Should_ThrowBadRequest_When_PendingRequestExists()
    {
        // Arrange
        var user = new User { UserId = 1, Role = UserRole.USER };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        var pending = new List<RoleChangeRequest>
        {
            new RoleChangeRequest { RequestId = 1, UserId = 1, Status = RequestStatus.PENDING }
        };
        _requestRepoMock.Setup(r => r.GetQueryable()).Returns(pending.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RequestOrganizerRoleAsync(1));
    }

    // ── GetPendingRequestsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetPendingRequestsAsync_Should_ReturnPendingRequests()
    {
        // Arrange
        var requests = new List<RoleChangeRequest>
        {
            new RoleChangeRequest { RequestId = 1, UserId = 1, Status = RequestStatus.PENDING },
            new RoleChangeRequest { RequestId = 2, UserId = 2, Status = RequestStatus.APPROVED }
        };
        _requestRepoMock.Setup(r => r.GetQueryable()).Returns(requests.BuildMock());

        // Act
        var result = (await _sut.GetPendingRequestsAsync()).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(RequestStatus.PENDING, result[0].Status);
    }

    // ── ApproveRequestAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ApproveRequestAsync_Should_PromoteUser_When_RequestIsPending()
    {
        // Arrange
        var request = new RoleChangeRequest { RequestId = 1, UserId = 2, Status = RequestStatus.PENDING };
        var user = new User { UserId = 2, Role = UserRole.USER };
        _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(2, It.IsAny<User>())).ReturnsAsync(user);
        _requestRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<RoleChangeRequest>())).ReturnsAsync(request);

        // Act
        var result = await _sut.ApproveRequestAsync(1, adminId: 99);

        // Assert
        Assert.Equal("User promoted to organizer", result);
    }

    [Fact]
    public async Task ApproveRequestAsync_Should_ThrowNotFound_When_RequestDoesNotExist()
    {
        // Arrange
        _requestRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RoleChangeRequest?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.ApproveRequestAsync(99, adminId: 1));
    }

    [Fact]
    public async Task ApproveRequestAsync_Should_ThrowBadRequest_When_RequestAlreadyProcessed()
    {
        // Arrange
        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new RoleChangeRequest { RequestId = 1, Status = RequestStatus.APPROVED });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.ApproveRequestAsync(1, adminId: 1));
    }

    // ── RejectRequestAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task RejectRequestAsync_Should_RejectRequest_When_RequestIsPending()
    {
        // Arrange
        var request = new RoleChangeRequest { RequestId = 1, Status = RequestStatus.PENDING };
        _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        _requestRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<RoleChangeRequest>())).ReturnsAsync(request);

        // Act
        var result = await _sut.RejectRequestAsync(1, adminId: 99);

        // Assert
        Assert.Equal("Request rejected", result);
    }

    [Fact]
    public async Task RejectRequestAsync_Should_ThrowNotFound_When_RequestDoesNotExist()
    {
        // Arrange
        _requestRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RoleChangeRequest?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RejectRequestAsync(99, adminId: 1));
    }

    [Fact]
    public async Task RejectRequestAsync_Should_ThrowBadRequest_When_RequestAlreadyProcessed()
    {
        // Arrange
        _requestRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new RoleChangeRequest { RequestId = 1, Status = RequestStatus.REJECTED });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RejectRequestAsync(1, adminId: 1));
    }
}


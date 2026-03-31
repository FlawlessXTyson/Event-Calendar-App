using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.User;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using Microsoft.AspNetCore.Http;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace EventCalenderApi.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IRepository<int, User>> _userRepoMock = new();
        private readonly Mock<IAuditLogRepository> _auditMock = new();
        private UserService CreateService() => new(_userRepoMock.Object, _auditMock.Object);

        // ── CreateUserAsync ──────────────────────────────────────────────

        [Fact]
        public async Task CreateUser_ValidRequest_ReturnsUser()
        {
            var users = new List<User>();
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(new User { UserId = 1, Name = "Alice", Email = "alice@test.com", Role = UserRole.USER });

            var result = await CreateService().CreateUserAsync(new CreateUserRequestDTO
            {
                Name = "Alice",
                Email = "alice@test.com",
                Password = "password123"
            });

            Assert.Equal("alice@test.com", result.Email);
        }

        [Fact]
        public async Task CreateUser_EmptyEmail_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CreateUserAsync(new CreateUserRequestDTO { Name = "A", Email = "", Password = "pass" }));
        }

        [Fact]
        public async Task CreateUser_EmptyPassword_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CreateUserAsync(new CreateUserRequestDTO { Name = "A", Email = "a@b.com", Password = "" }));
        }

        [Fact]
        public async Task CreateUser_DuplicateEmail_ThrowsBadRequest()
        {
            var users = new List<User> { new() { UserId = 1, Email = "alice@test.com" } };
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CreateUserAsync(new CreateUserRequestDTO
                {
                    Name = "Alice2",
                    Email = "alice@test.com",
                    Password = "pass123"
                }));
        }

        // ── GetUserByIdAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GetUserById_Found_ReturnsUser()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new User { UserId = 1, Name = "Alice", Email = "alice@test.com" });

            var result = await CreateService().GetUserByIdAsync(1);
            Assert.Equal(1, result.UserId);
        }

        [Fact]
        public async Task GetUserById_NotFound_ThrowsNotFound()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetUserByIdAsync(99));
        }

        // ── GetAllUsersAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GetAllUsers_ReturnsAll()
        {
            var users = new List<User>
            {
                new() { UserId = 1, Name = "Alice" },
                new() { UserId = 2, Name = "Bob" }
            };
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

            var result = (await CreateService().GetAllUsersAsync()).ToList();
            Assert.Equal(2, result.Count);
        }

        // ── UpdateUserAsync ──────────────────────────────────────────────

        [Fact]
        public async Task UpdateUser_ValidRequest_ReturnsUpdated()
        {
            var user = new User { UserId = 1, Name = "Alice", Email = "alice@test.com" };
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var noConflict = new List<User>();
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(noConflict.BuildMock());
            _userRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<User>()))
                .ReturnsAsync(new User { UserId = 1, Name = "Alice Updated", Email = "new@test.com" });

            var result = await CreateService().UpdateUserAsync(1, new UpdateUserRequestDTO
            {
                Name = "Alice Updated",
                Email = "new@test.com"
            });

            Assert.Equal("Alice Updated", result.Name);
        }

        [Fact]
        public async Task UpdateUser_NotFound_ThrowsNotFound()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                CreateService().UpdateUserAsync(99, new UpdateUserRequestDTO()));
        }

        [Fact]
        public async Task UpdateUser_DuplicateEmail_ThrowsBadRequest()
        {
            var user = new User { UserId = 1, Email = "alice@test.com" };
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var conflict = new List<User> { new() { UserId = 2, Email = "taken@test.com" } };
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(conflict.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().UpdateUserAsync(1, new UpdateUserRequestDTO { Email = "taken@test.com" }));
        }

        // ── DeleteUserAsync ──────────────────────────────────────────────

        [Fact]
        public async Task DeleteUser_Found_Deletes()
        {
            var user = new User { UserId = 1 };
            _userRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(user);
            await CreateService().DeleteUserAsync(1); // no exception = pass
        }

        [Fact]
        public async Task DeleteUser_NotFound_ThrowsNotFound()
        {
            _userRepoMock.Setup(r => r.DeleteAsync(99)).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().DeleteUserAsync(99));
        }

        // ── DisableUserAsync ─────────────────────────────────────────────

        [Fact]
        public async Task DisableUser_Success_SetsBlocked()
        {
            var user = new User { UserId = 2, Status = AccountStatus.ACTIVE };
            _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.UpdateAsync(2, user)).ReturnsAsync(user);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().DisableUserAsync(2, 1);
            Assert.Equal(AccountStatus.BLOCKED, result.Status);
        }

        [Fact]
        public async Task DisableUser_SameAsAdmin_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().DisableUserAsync(1, 1));
        }

        [Fact]
        public async Task DisableUser_NotFound_ThrowsNotFound()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().DisableUserAsync(99, 1));
        }

        [Fact]
        public async Task DisableUser_AlreadyBlocked_ThrowsBadRequest()
        {
            var user = new User { UserId = 2, Status = AccountStatus.BLOCKED };
            _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().DisableUserAsync(2, 1));
        }

        // ── EnableUserAsync ──────────────────────────────────────────────

        [Fact]
        public async Task EnableUser_Success_SetsActive()
        {
            var user = new User { UserId = 2, Status = AccountStatus.BLOCKED };
            _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.UpdateAsync(2, user)).ReturnsAsync(user);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().EnableUserAsync(2, 1);
            Assert.Equal(AccountStatus.ACTIVE, result.Status);
        }

        [Fact]
        public async Task EnableUser_NotFound_ThrowsNotFound()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().EnableUserAsync(99, 1));
        }

        [Fact]
        public async Task EnableUser_AlreadyActive_ThrowsBadRequest()
        {
            var user = new User { UserId = 2, Status = AccountStatus.ACTIVE };
            _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().EnableUserAsync(2, 1));
        }

        // ── UploadProfileImageAsync ──────────────────────────────────────

        [Fact]
        public async Task UploadProfileImage_InvalidExtension_ThrowsBadRequest()
        {
            var user = new User { UserId = 1 };
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("photo.gif");
            fileMock.Setup(f => f.Length).Returns(1024);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().UploadProfileImageAsync(1, fileMock.Object));
        }

        [Fact]
        public async Task UploadProfileImage_TooLarge_ThrowsBadRequest()
        {
            var user = new User { UserId = 1 };
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("photo.jpg");
            fileMock.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6 MB

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().UploadProfileImageAsync(1, fileMock.Object));
        }

        [Fact]
        public async Task UploadProfileImage_UserNotFound_ThrowsNotFound()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("photo.jpg");
            fileMock.Setup(f => f.Length).Returns(1024);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                CreateService().UploadProfileImageAsync(99, fileMock.Object));
        }

        // ── UpdateUserAsync — no email change ────────────────────────────

        [Fact]
        public async Task UpdateUser_NoEmailChange_OnlyUpdatesNameAndRole()
        {
            var user = new User { UserId = 1, Name = "Alice", Email = "alice@test.com", Role = UserRole.USER };
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<User>()))
                .ReturnsAsync(new User { UserId = 1, Name = "Alice Updated", Email = "alice@test.com", Role = UserRole.ORGANIZER });

            var result = await CreateService().UpdateUserAsync(1, new UpdateUserRequestDTO
            {
                Name = "Alice Updated",
                Role = UserRole.ORGANIZER
                // Email is null — no email change
            });

            Assert.Equal("Alice Updated", result.Name);
            // GetQueryable should NOT be called since no email was provided
            _userRepoMock.Verify(r => r.GetQueryable(), Times.Never);
        }

        // ── GetAllUsersAsync — empty ─────────────────────────────────────

        [Fact]
        public async Task GetAllUsers_Empty_ReturnsEmpty()
        {
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(new List<User>().BuildMock());
            var result = await CreateService().GetAllUsersAsync();
            Assert.Empty(result);
        }

        // ── DisableUserAsync — audit log fields ──────────────────────────

        [Fact]
        public async Task DisableUser_AuditLog_HasCorrectFields()
        {
            var user = new User { UserId = 2, Status = AccountStatus.ACTIVE };
            _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.UpdateAsync(2, user)).ReturnsAsync(user);

            AuditLog? captured = null;
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => captured = log)
                .ReturnsAsync(new AuditLog());

            await CreateService().DisableUserAsync(2, 1);

            Assert.Equal(1, captured?.UserId);
            Assert.Equal("DISABLE_USER", captured?.Action);
            Assert.Equal(2, captured?.EntityId);
        }

        // ── EnableUserAsync — audit log fields ───────────────────────────

        [Fact]
        public async Task EnableUser_AuditLog_HasCorrectFields()
        {
            var user = new User { UserId = 2, Status = AccountStatus.BLOCKED };
            _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.UpdateAsync(2, user)).ReturnsAsync(user);

            AuditLog? captured = null;
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => captured = log)
                .ReturnsAsync(new AuditLog());

            await CreateService().EnableUserAsync(2, 1);

            Assert.Equal(1, captured?.UserId);
            Assert.Equal("ENABLE_USER", captured?.Action);
            Assert.Equal(2, captured?.EntityId);
        }

        // ── CreateUserAsync — password is hashed ─────────────────────────

        [Fact]
        public async Task CreateUser_PasswordIsHashed_NotStoredPlain()
        {
            var users = new List<User>();
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

            User? capturedUser = null;
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync((User u) => u);

            await CreateService().CreateUserAsync(new CreateUserRequestDTO
            {
                Name = "Alice",
                Email = "alice@test.com",
                Password = "plaintext"
            });

            Assert.NotEqual("plaintext", capturedUser?.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("plaintext", capturedUser?.PasswordHash));
        }

        // ── UpdateUserAsync — status-only update ─────────────────────────

        [Fact]
        public async Task UpdateUser_StatusOnly_UpdatesWithoutEmailCheck()
        {
            var user = new User { UserId = 1, Name = "Alice", Email = "alice@test.com", Role = UserRole.USER, Status = AccountStatus.ACTIVE };
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<User>()))
                .ReturnsAsync(new User { UserId = 1, Name = "Alice", Email = "alice@test.com", Status = AccountStatus.BLOCKED });

            var result = await CreateService().UpdateUserAsync(1, new UpdateUserRequestDTO
            {
                Status = AccountStatus.BLOCKED
                // No email, no name, no role
            });

            Assert.Equal(AccountStatus.BLOCKED, result.Status);
            // GetQueryable should NOT be called (no email change)
            _userRepoMock.Verify(r => r.GetQueryable(), Times.Never);
        }

        // ── CreateUserAsync — email normalized to lowercase ──────────────

        [Fact]
        public async Task CreateUser_EmailNormalized_ToLowercase()
        {
            var users = new List<User>();
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

            User? capturedUser = null;
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync((User u) => u);

            await CreateService().CreateUserAsync(new CreateUserRequestDTO
            {
                Name = "Alice",
                Email = "ALICE@TEST.COM",
                Password = "pass123"
            });

            Assert.Equal("alice@test.com", capturedUser?.Email);
        }
    }
}






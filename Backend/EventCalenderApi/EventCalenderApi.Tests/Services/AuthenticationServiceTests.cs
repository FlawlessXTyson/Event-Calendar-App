using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using Microsoft.Extensions.Configuration;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace EventCalenderApi.Tests.Services
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<IRepository<int, User>> _userRepoMock = new();
        private readonly Mock<IAuditLogRepository> _auditMock = new();

        private IConfiguration BuildConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "SuperSecretTestKeyThatIsLongEnough1234567890",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                    ["Jwt:DurationInMinutes"] = "60"
                })
                .Build();

        private AuthenticationService CreateService() =>
            new(_userRepoMock.Object, BuildConfig(), _auditMock.Object);

        // ── RegisterAsync ────────────────────────────────────────────────

        [Fact]
        public async Task Register_ValidRequest_ReturnsToken()
        {
            var users = new List<User>();
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(new User { UserId = 1, Name = "Alice", Email = "alice@test.com", Role = UserRole.USER });
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().RegisterAsync(new RegisterRequestDTO
            {
                UserName = "Alice",
                Email = "alice@test.com",
                Password = "password123"
            });

            Assert.NotNull(result.Token);
            Assert.NotEmpty(result.Token);
        }

        [Fact]
        public async Task Register_EmptyEmail_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().RegisterAsync(new RegisterRequestDTO
                {
                    UserName = "Alice",
                    Email = "",
                    Password = "password123"
                }));
        }

        [Fact]
        public async Task Register_EmptyPassword_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().RegisterAsync(new RegisterRequestDTO
                {
                    UserName = "Alice",
                    Email = "alice@test.com",
                    Password = ""
                }));
        }

        [Fact]
        public async Task Register_DuplicateEmail_ThrowsBadRequest()
        {
            var users = new List<User>
            {
                new() { UserId = 1, Email = "alice@test.com" }
            };
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().RegisterAsync(new RegisterRequestDTO
                {
                    UserName = "Alice2",
                    Email = "alice@test.com",
                    Password = "password123"
                }));
        }

        [Fact]
        public async Task Register_EmailNormalized_ToLowercase()
        {
            var users = new List<User>();
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

            User? capturedUser = null;
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync((User u) => u);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().RegisterAsync(new RegisterRequestDTO
            {
                UserName = "Alice",
                Email = "ALICE@TEST.COM",
                Password = "password123"
            });

            Assert.Equal("alice@test.com", capturedUser?.Email);
        }

        // ── LoginAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("password123");
            var users = new List<User>
            {
                new() { UserId = 1, Email = "alice@test.com", PasswordHash = hash, Status = AccountStatus.ACTIVE, Role = UserRole.USER, Name = "Alice" }
            };
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().LoginAsync(new LoginRequestDTO
            {
                Email = "alice@test.com",
                Password = "password123"
            });

            Assert.NotNull(result.Token);
        }

        [Fact]
        public async Task Login_EmptyEmail_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().LoginAsync(new LoginRequestDTO { Email = "", Password = "pass" }));
        }

        [Fact]
        public async Task Login_EmptyPassword_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().LoginAsync(new LoginRequestDTO { Email = "a@b.com", Password = "" }));
        }

        [Fact]
        public async Task Login_UserNotFound_ThrowsUnauthorized()
        {
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(new List<User>().BuildMock());

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                CreateService().LoginAsync(new LoginRequestDTO { Email = "nobody@test.com", Password = "pass" }));
        }

        [Fact]
        public async Task Login_WrongPassword_ThrowsUnauthorized()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("correctpass");
            var users = new List<User>
            {
                new() { UserId = 1, Email = "alice@test.com", PasswordHash = hash, Status = AccountStatus.ACTIVE }
            };
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                CreateService().LoginAsync(new LoginRequestDTO { Email = "alice@test.com", Password = "wrongpass" }));
        }

        [Fact]
        public async Task Login_BlockedAccount_ThrowsUnauthorized()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("password123");
            var users = new List<User>
            {
                new() { UserId = 1, Email = "alice@test.com", PasswordHash = hash, Status = AccountStatus.BLOCKED }
            };
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                CreateService().LoginAsync(new LoginRequestDTO { Email = "alice@test.com", Password = "password123" }));
        }

        // ── GenerateTokenResponse — missing JWT config ───────────────────

        [Fact]
        public async Task Register_MissingJwtKey_ThrowsBadRequest()
        {
            var configWithoutKey = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "TestIssuer"
                    // No Jwt:Key
                })
                .Build();

            var svc = new AuthenticationService(_userRepoMock.Object, configWithoutKey, _auditMock.Object);

            var users = new List<User>();
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(new User { UserId = 1, Name = "Alice", Email = "alice@test.com", Role = UserRole.USER });
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                svc.RegisterAsync(new RegisterRequestDTO
                {
                    UserName = "Alice",
                    Email = "alice@test.com",
                    Password = "password123"
                }));
        }

        // ── Login — email normalization ──────────────────────────────────

        [Fact]
        public async Task Login_EmailNormalized_ToLowercase()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("password123");
            var users = new List<User>
            {
                new() { UserId = 1, Email = "alice@test.com", PasswordHash = hash, Status = AccountStatus.ACTIVE, Role = UserRole.USER, Name = "Alice" }
            };
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            // Login with uppercase email — should still find the user
            var result = await CreateService().LoginAsync(new LoginRequestDTO
            {
                Email = "ALICE@TEST.COM",
                Password = "password123"
            });

            Assert.NotNull(result.Token);
        }
    }
}






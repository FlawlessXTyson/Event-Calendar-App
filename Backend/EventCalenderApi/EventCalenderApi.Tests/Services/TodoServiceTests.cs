using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Todo;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace EventCalenderApi.Tests.Services
{
    public class TodoServiceTests
    {
        private readonly Mock<IRepository<int, Todo>> _repoMock = new();
        private readonly Mock<IAuditLogRepository> _auditMock = new();
        private TodoService CreateService() => new(_repoMock.Object, _auditMock.Object);

        // ── CreateAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ValidRequest_ReturnsTodo()
        {
            var dto = new CreateTodoRequestDTO { UserId = 1, TaskTitle = "Test Task" };
            var todo = new Todo { TodoId = 1, UserId = 1, TaskTitle = "Test Task", Status = TodoStatus.PENDING };

            _repoMock.Setup(r => r.AddAsync(It.IsAny<Todo>())).ReturnsAsync(todo);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CreateAsync(dto);

            Assert.Equal("Test Task", result.TaskTitle);
            Assert.Equal(TodoStatus.PENDING, result.Status);
        }

        [Fact]
        public async Task CreateAsync_EmptyTitle_ThrowsBadRequest()
        {
            var dto = new CreateTodoRequestDTO { UserId = 1, TaskTitle = "   " };
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_PastDueDate_ThrowsBadRequest()
        {
            var dto = new CreateTodoRequestDTO
            {
                UserId = 1,
                TaskTitle = "Task",
                DueDate = DateTime.UtcNow.Date.AddDays(-1)
            };
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_FutureDueDate_Succeeds()
        {
            var dto = new CreateTodoRequestDTO
            {
                UserId = 1,
                TaskTitle = "Task",
                DueDate = DateTime.UtcNow.Date.AddDays(1)
            };
            var todo = new Todo { TodoId = 1, UserId = 1, TaskTitle = "Task", DueDate = dto.DueDate };
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Todo>())).ReturnsAsync(todo);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CreateAsync(dto);
            Assert.Equal(dto.DueDate, result.DueDate);
        }

        // ── GetByUserAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetByUserAsync_ReturnsTodosForUser()
        {
            var todos = new List<Todo>
            {
                new() { TodoId = 1, UserId = 1, TaskTitle = "A", CreatedAt = DateTime.UtcNow },
                new() { TodoId = 2, UserId = 1, TaskTitle = "B", CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
            };
            _repoMock.Setup(r => r.GetQueryable()).Returns(todos.BuildMock());

            var result = (await CreateService().GetByUserAsync(1)).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByUserAsync_NoTodos_ReturnsEmpty()
        {
            _repoMock.Setup(r => r.GetQueryable()).Returns(new List<Todo>().BuildMock());
            var result = await CreateService().GetByUserAsync(99);
            Assert.Empty(result);
        }

        // ── MarkCompletedAsync ───────────────────────────────────────────

        [Fact]
        public async Task MarkCompletedAsync_ValidTodo_Completes()
        {
            var todo = new Todo { TodoId = 1, UserId = 1, Status = TodoStatus.PENDING };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(todo);
            _repoMock.Setup(r => r.UpdateAsync(1, todo)).ReturnsAsync(todo);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().MarkCompletedAsync(1, 1);

            Assert.Equal(TodoStatus.COMPLETED, todo.Status);
        }

        [Fact]
        public async Task MarkCompletedAsync_NotFound_ThrowsNotFound()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Todo?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().MarkCompletedAsync(99, 1));
        }

        [Fact]
        public async Task MarkCompletedAsync_WrongUser_ThrowsUnauthorized()
        {
            var todo = new Todo { TodoId = 1, UserId = 2, Status = TodoStatus.PENDING };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(todo);
            await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().MarkCompletedAsync(1, 1));
        }

        [Fact]
        public async Task MarkCompletedAsync_AlreadyCompleted_ThrowsBadRequest()
        {
            var todo = new Todo { TodoId = 1, UserId = 1, Status = TodoStatus.COMPLETED };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(todo);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().MarkCompletedAsync(1, 1));
        }

        // ── UpdateAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ValidRequest_ReturnsUpdated()
        {
            var todo = new Todo { TodoId = 1, UserId = 1, TaskTitle = "Old" };
            var dto = new UpdateTodoRequestDTO { TaskTitle = "New", DueDate = DateTime.UtcNow.Date.AddDays(2) };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(todo);
            _repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Todo>())).ReturnsAsync(new Todo { TodoId = 1, UserId = 1, TaskTitle = "New", DueDate = dto.DueDate });
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().UpdateAsync(1, 1, dto);
            Assert.Equal("New", result.TaskTitle);
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ThrowsNotFound()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Todo?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                CreateService().UpdateAsync(99, 1, new UpdateTodoRequestDTO { TaskTitle = "X" }));
        }

        [Fact]
        public async Task UpdateAsync_WrongUser_ThrowsUnauthorized()
        {
            var todo = new Todo { TodoId = 1, UserId = 2 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(todo);
            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                CreateService().UpdateAsync(1, 1, new UpdateTodoRequestDTO { TaskTitle = "X" }));
        }

        [Fact]
        public async Task UpdateAsync_EmptyTitle_ThrowsBadRequest()
        {
            var todo = new Todo { TodoId = 1, UserId = 1 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(todo);
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().UpdateAsync(1, 1, new UpdateTodoRequestDTO { TaskTitle = "" }));
        }

        [Fact]
        public async Task UpdateAsync_PastDueDate_ThrowsBadRequest()
        {
            var todo = new Todo { TodoId = 1, UserId = 1 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(todo);
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().UpdateAsync(1, 1, new UpdateTodoRequestDTO
                {
                    TaskTitle = "X",
                    DueDate = DateTime.UtcNow.Date.AddDays(-1)
                }));
        }

        // ── DeleteAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ValidRequest_Deletes()
        {
            var todo = new Todo { TodoId = 1, UserId = 1 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(todo);
            _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(todo);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().DeleteAsync(1, 1); // no exception = pass
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFound()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Todo?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().DeleteAsync(99, 1));
        }

        [Fact]
        public async Task DeleteAsync_WrongUser_ThrowsUnauthorized()
        {
            var todo = new Todo { TodoId = 1, UserId = 2 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(todo);
            await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().DeleteAsync(1, 1));
        }

        // ── CreateAsync — null due date ──────────────────────────────────

        [Fact]
        public async Task CreateAsync_NullDueDate_Succeeds()
        {
            var dto = new CreateTodoRequestDTO { UserId = 1, TaskTitle = "No deadline" };
            var todo = new Todo { TodoId = 1, UserId = 1, TaskTitle = "No deadline", DueDate = null };
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Todo>())).ReturnsAsync(todo);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CreateAsync(dto);
            Assert.Null(result.DueDate);
        }

        // ── CreateAsync — audit log fields ───────────────────────────────

        [Fact]
        public async Task CreateAsync_AuditLog_HasCorrectAction()
        {
            var dto = new CreateTodoRequestDTO { UserId = 5, TaskTitle = "Task" };
            var todo = new Todo { TodoId = 1, UserId = 5, TaskTitle = "Task" };
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Todo>())).ReturnsAsync(todo);

            AuditLog? captured = null;
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => captured = log)
                .ReturnsAsync(new AuditLog());

            await CreateService().CreateAsync(dto);

            Assert.Equal("CREATE_TODO", captured?.Action);
            Assert.Equal(5, captured?.UserId);
        }

        // ── UpdateAsync — null due date ──────────────────────────────────

        [Fact]
        public async Task UpdateAsync_NullDueDate_Succeeds()
        {
            var todo = new Todo { TodoId = 1, UserId = 1, TaskTitle = "Old" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(todo);
            _repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Todo>()))
                .ReturnsAsync(new Todo { TodoId = 1, UserId = 1, TaskTitle = "New", DueDate = null });
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().UpdateAsync(1, 1, new UpdateTodoRequestDTO { TaskTitle = "New" });
            Assert.Null(result.DueDate);
        }

        // ── MarkCompletedAsync — audit log ───────────────────────────────

        [Fact]
        public async Task MarkCompletedAsync_AuditLog_HasCorrectAction()
        {
            var todo = new Todo { TodoId = 3, UserId = 7, Status = TodoStatus.PENDING };
            _repoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(todo);
            _repoMock.Setup(r => r.UpdateAsync(3, todo)).ReturnsAsync(todo);

            AuditLog? captured = null;
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => captured = log)
                .ReturnsAsync(new AuditLog());

            await CreateService().MarkCompletedAsync(3, 7);

            Assert.Equal("COMPLETE_TODO", captured?.Action);
            Assert.Equal(7, captured?.UserId);
        }
    }
}






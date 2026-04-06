using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Todo;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Services;

public class TodoServiceTests
{
    private readonly Mock<IRepository<int, Todo>> _repoMock = new();
    private readonly Mock<IAuditLogRepository> _auditRepoMock = new();
    private readonly TodoService _sut;

    public TodoServiceTests()
    {
        _sut = new TodoService(_repoMock.Object, _auditRepoMock.Object);
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());
    }

    // ── CreateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Should_ReturnTodo_When_ValidRequest()
    {
        // Arrange
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Todo>()))
            .ReturnsAsync((Todo t) => { t.TodoId = 1; return t; });

        var dto = new CreateTodoRequestDTO { UserId = 1, TaskTitle = "Buy groceries", DueDate = DateTime.UtcNow.Date.AddDays(1) };

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        Assert.Equal("Buy groceries", result.TaskTitle);
        Assert.Equal(TodoStatus.PENDING, result.Status);
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowBadRequest_When_TitleIsEmpty()
    {
        // Arrange
        var dto = new CreateTodoRequestDTO { UserId = 1, TaskTitle = "   " };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowBadRequest_When_DueDateIsInPast()
    {
        // Arrange
        var dto = new CreateTodoRequestDTO { UserId = 1, TaskTitle = "Task", DueDate = DateTime.UtcNow.Date.AddDays(-1) };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(dto));
    }

    // ── GetByUserAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserAsync_Should_ReturnUserTodos_When_TodosExist()
    {
        // Arrange
        var todos = new List<Todo>
        {
            new Todo { TodoId = 1, UserId = 1, TaskTitle = "Task A" },
            new Todo { TodoId = 2, UserId = 2, TaskTitle = "Task B" }
        };
        _repoMock.Setup(r => r.GetQueryable()).Returns(todos.BuildMock());

        // Act
        var result = (await _sut.GetByUserAsync(1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("Task A", result[0].TaskTitle);
    }

    // ── MarkCompletedAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task MarkCompletedAsync_Should_Complete_When_TodoBelongsToUser()
    {
        // Arrange
        var todo = new Todo { TodoId = 1, UserId = 1, Status = TodoStatus.PENDING };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(todo);
        _repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Todo>())).ReturnsAsync(todo);

        // Act
        await _sut.MarkCompletedAsync(1, 1);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(1, It.IsAny<Todo>()), Times.Once);
    }

    [Fact]
    public async Task MarkCompletedAsync_Should_ThrowNotFound_When_TodoDoesNotExist()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Todo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.MarkCompletedAsync(99, 1));
    }

    [Fact]
    public async Task MarkCompletedAsync_Should_ThrowUnauthorized_When_TodoBelongsToOtherUser()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Todo { TodoId = 1, UserId = 2 });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.MarkCompletedAsync(1, userId: 1));
    }

    [Fact]
    public async Task MarkCompletedAsync_Should_ThrowBadRequest_When_AlreadyCompleted()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Todo { TodoId = 1, UserId = 1, Status = TodoStatus.COMPLETED });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.MarkCompletedAsync(1, 1));
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_Should_ReturnUpdatedTodo_When_ValidRequest()
    {
        // Arrange
        var todo = new Todo { TodoId = 1, UserId = 1, TaskTitle = "Old Title" };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(todo);
        _repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Todo>())).ReturnsAsync(todo);

        var dto = new UpdateTodoRequestDTO { TaskTitle = "New Title" };

        // Act
        var result = await _sut.UpdateAsync(1, 1, dto);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateAsync_Should_ThrowBadRequest_When_TitleIsEmpty()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Todo { TodoId = 1, UserId = 1 });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateAsync(1, 1, new UpdateTodoRequestDTO { TaskTitle = "" }));
    }

    [Fact]
    public async Task UpdateAsync_Should_ThrowUnauthorized_When_TodoBelongsToOtherUser()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Todo { TodoId = 1, UserId = 2 });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.UpdateAsync(1, userId: 1, new UpdateTodoRequestDTO { TaskTitle = "Title" }));
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Should_Delete_When_TodoBelongsToUser()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Todo { TodoId = 1, UserId = 1 });
        _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(new Todo { TodoId = 1 });

        // Act
        await _sut.DeleteAsync(1, 1);

        // Assert
        _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Should_ThrowNotFound_When_TodoDoesNotExist()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Todo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99, 1));
    }

    [Fact]
    public async Task DeleteAsync_Should_ThrowUnauthorized_When_TodoBelongsToOtherUser()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Todo { TodoId = 1, UserId = 2 });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.DeleteAsync(1, userId: 1));
    }
}


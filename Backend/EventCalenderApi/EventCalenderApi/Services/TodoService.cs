using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Todo;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class TodoService : ITodoService
    {
        private readonly IRepository<int, Todo> _repo;
        private readonly IAuditLogRepository _auditRepo;

        public TodoService(
            IRepository<int, Todo> repo,
            IAuditLogRepository auditRepo)
        {
            _repo = repo;
            _auditRepo = auditRepo;
        }

        // ================= CREATE =================
        public async Task<CreateTodoResponseDTO> CreateAsync(CreateTodoRequestDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TaskTitle))
                throw new BadRequestException("Task title is required");

            if (dto.DueDate.HasValue && dto.DueDate.Value < DateTime.UtcNow.Date)
                throw new BadRequestException("Due date cannot be in the past");

            var todo = new Todo
            {
                UserId = dto.UserId,
                TaskTitle = dto.TaskTitle,
                DueDate = dto.DueDate,
                Status = TodoStatus.PENDING,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repo.AddAsync(todo);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = dto.UserId,
                Role = "USER",
                Action = "CREATE_TODO",
                Entity = "Todo",
                EntityId = created.TodoId
            });

            return MapToDTO(created);
        }

        // ================= GET =================
        public async Task<IEnumerable<CreateTodoResponseDTO>> GetByUserAsync(int userId)
        {
            var todos = await _repo
                .GetQueryable()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return todos.Select(MapToDTO);
        }

        // ================= COMPLETE =================
        public async Task MarkCompletedAsync(int todoId, int userId)
        {
            var todo = await _repo.GetByIdAsync(todoId)
                ?? throw new NotFoundException("Todo not found");

            if (todo.UserId != userId)
                throw new UnauthorizedException("You can update only your own todos");

            if (todo.Status == TodoStatus.COMPLETED)
                throw new BadRequestException("Todo already completed");

            todo.Status = TodoStatus.COMPLETED;

            await _repo.UpdateAsync(todoId, todo);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Role = "USER",
                Action = "COMPLETE_TODO",
                Entity = "Todo",
                EntityId = todoId
            });
        }

        // ================= UPDATE =================
        public async Task<CreateTodoResponseDTO> UpdateAsync(int todoId, int userId, UpdateTodoRequestDTO dto)
        {
            var todo = await _repo.GetByIdAsync(todoId)
                ?? throw new NotFoundException("Todo not found");

            if (todo.UserId != userId)
                throw new UnauthorizedException("You can update only your own todos");

            if (string.IsNullOrWhiteSpace(dto.TaskTitle))
                throw new BadRequestException("Task title is required");

            if (dto.DueDate.HasValue && dto.DueDate.Value < DateTime.UtcNow.Date)
                throw new BadRequestException("Due date cannot be in the past");

            todo.TaskTitle = dto.TaskTitle;
            todo.DueDate = dto.DueDate;

            var updated = await _repo.UpdateAsync(todoId, todo);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Role = "USER",
                Action = "UPDATE_TODO",
                Entity = "Todo",
                EntityId = todoId
            });

            return MapToDTO(updated!);
        }

        // ================= DELETE =================
        public async Task DeleteAsync(int todoId, int userId)
        {
            var todo = await _repo.GetByIdAsync(todoId)
                ?? throw new NotFoundException("Todo not found");

            if (todo.UserId != userId)
                throw new UnauthorizedException("You can delete only your own todos");

            await _repo.DeleteAsync(todoId);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Role = "USER",
                Action = "DELETE_TODO",
                Entity = "Todo",
                EntityId = todoId
            });
        }

        // ================= MAPPER =================
        private static CreateTodoResponseDTO MapToDTO(Todo t)
        {
            return new CreateTodoResponseDTO
            {
                TodoId = t.TodoId,
                UserId = t.UserId,
                TaskTitle = t.TaskTitle,
                DueDate = t.DueDate,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            };
        }
    }
}
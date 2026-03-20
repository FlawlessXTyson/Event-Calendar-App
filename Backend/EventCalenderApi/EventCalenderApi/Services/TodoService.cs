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

        //constructor injection
        public TodoService(IRepository<int, Todo> repo)
        {
            _repo = repo;
        }

        //create todo
        public async Task<CreateTodoResponseDTO> CreateAsync(CreateTodoRequestDTO dto)
        {
            //validate title
            if (string.IsNullOrWhiteSpace(dto.TaskTitle))
                throw new BadRequestException("Task title is required");

            //validate due date
            if (dto.DueDate < DateTime.UtcNow.Date)
                throw new BadRequestException("Due date cannot be in the past");

            //create entity
            var todo = new Todo
            {
                UserId = dto.UserId,
                TaskTitle = dto.TaskTitle,
                DueDate = dto.DueDate,
                Status = TodoStatus.PENDING,
                CreatedAt = DateTime.UtcNow
            };

            //save to database
            var created = await _repo.AddAsync(todo);

            //map to dto
            return MapToDTO(created);
        }

        //get todos for user
        public async Task<IEnumerable<CreateTodoResponseDTO>> GetByUserAsync(int userId)
        {
            var todos = await _repo
                .GetQueryable()
                .Where(t => t.UserId == userId)
                .ToListAsync();

            //map list
            return todos.Select(MapToDTO);
        }

        //mark todo as completed
        public async Task MarkCompletedAsync(int todoId, int userId)
        {
            //fetch todo
            var todo = await _repo.GetByIdAsync(todoId)
                ?? throw new NotFoundException("Todo not found");

            //ownership check
            if (todo.UserId != userId)
                throw new UnauthorizedException("You can update only your own todos");

            //prevent duplicate completion
            if (todo.Status == TodoStatus.COMPLETED)
                throw new BadRequestException("Todo already completed");

            //update status
            todo.Status = TodoStatus.COMPLETED;

            await _repo.UpdateAsync(todoId, todo);
        }

        //helper mapping method
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
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

        public TodoService(IRepository<int, Todo> repo)
        {
            _repo = repo;
        }

        //create todo
        public async Task<CreateTodoResponseDTO> CreateAsync(CreateTodoRequestDTO dto)
        {
            var todo = new Todo
            {
                UserId = dto.UserId,
                TaskTitle = dto.TaskTitle,
                DueDate = dto.DueDate,
                Status = TodoStatus.PENDING,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repo.AddAsync(todo);

            return new CreateTodoResponseDTO
            {
                TodoId = created.TodoId,
                UserId = created.UserId,
                TaskTitle = created.TaskTitle,
                DueDate = created.DueDate,
                Status = created.Status,
                CreatedAt = created.CreatedAt
            };
        }

        //get todos
        public async Task<IEnumerable<CreateTodoResponseDTO>> GetByUserAsync(int userId)
        {
            var todos = await _repo
                .GetQueryable()
                .Where(t => t.UserId == userId)
                .ToListAsync();

            return todos.Select(t => new CreateTodoResponseDTO
            {
                TodoId = t.TodoId,
                UserId = t.UserId,
                TaskTitle = t.TaskTitle,
                DueDate = t.DueDate,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            });
        }

        //mark completed
        public async Task MarkCompletedAsync(int todoId)
        {
            var todo = await _repo.GetByIdAsync(todoId);

            if (todo == null)
                throw new NotFoundException("Todo not found");

            todo.Status = TodoStatus.COMPLETED;

            await _repo.UpdateAsync(todoId, todo);
        }
    }
}
using System.Text.Json;
using EventCalenderApi.Exceptions;
using EventCalenderApi.EventCalenderAppDataLibrary;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models;

namespace EventCalenderApi.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                int statusCode = ex is AppException appEx
                    ? appEx.StatusCode
                    : StatusCodes.Status500InternalServerError;

                // 🔥 SAVE ERROR TO DATABASE
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider
                        .GetRequiredService<EventCalendarDbContext>();

                    var log = new ErrorLog
                    {
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                        Path = context.Request.Path,
                        Method = context.Request.Method,
                        StatusCode = statusCode,
                        CreatedAt = DateTime.UtcNow
                    };

                    dbContext.ErrorLogs.Add(log);
                    await dbContext.SaveChangesAsync();
                }

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = statusCode;

                var response = new
                {
                    success = false,
                    statusCode = statusCode,
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(json);
            }
        }
    }
}
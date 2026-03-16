using System.Text.Json;
using EventCalenderApi.Exceptions;

namespace EventCalenderApi.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next,
                                   ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);// used to store the log int the logger 

                context.Response.ContentType = "application/json";// we are setting the response type 

                var statusCode = ex switch
                {
                    BadRequestException => StatusCodes.Status400BadRequest,//400
                    NotFoundException => StatusCodes.Status404NotFound,//404
                    UnauthorizedException => StatusCodes.Status401Unauthorized,//401
                    _ => StatusCodes.Status500InternalServerError
                };

                context.Response.StatusCode = statusCode;

                var response = new
                {
                    statusCode,
                    message = ex.Message
                };

                var json = JsonSerializer.Serialize(response);

                await context.Response.WriteAsync(json);
            }
        }
    }
}
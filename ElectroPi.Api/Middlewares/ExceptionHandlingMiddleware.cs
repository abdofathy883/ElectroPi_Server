using ElectroPi.Domain.Exceptions;

namespace ElectroPi.Api.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            context.Response.StatusCode = exception switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedException => StatusCodes.Status401Unauthorized,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                ForbiddenException => StatusCodes.Status403Forbidden,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                NotImplementedException => StatusCodes.Status501NotImplemented,
                _ => StatusCodes.Status500InternalServerError
            };

            if (context.Response.StatusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception occurred");
            }

            await context.Response.WriteAsJsonAsync(new
            {
                message = exception.Message
            });
        }
    }
}

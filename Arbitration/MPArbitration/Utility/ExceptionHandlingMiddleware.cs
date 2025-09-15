using Newtonsoft.Json;
using System.Net;

namespace MPArbitration.Utility
{

    /// <summary>
    /// Entity to handle exception middle ware
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        /// <summary>
        /// Constructor to initialize objects
        /// </summary>
        /// <param name="next"> RequestDelegate object </param>
        /// <param name="logger">ILogger object</param>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Method to handle exceptions
        /// </summary>
        /// <param name="httpContext">HttpContext object</param>
        /// <returns> Actual response if there is no exception else exception message with Status500InternalServerError </returns>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError($"Unknown exception: {exception}");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var result = JsonConvert.SerializeObject(new { Exception = exception.Message });
            return context.Response.WriteAsync(result);
        }
    }
}


using System.Net;
using System.Text.Json;

namespace Zumlo.Gateway.Api.Middleware
{
    public class ErrorEnvelopeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorEnvelopeMiddleware> _logger;
        public ErrorEnvelopeMiddleware(RequestDelegate next, ILogger<ErrorEnvelopeMiddleware> logger)
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
                var traceId = context.TraceIdentifier;
                _logger.LogError(ex, "Unhandled exception {TraceId}", traceId);

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var payload = new
                {
                    error = new
                    {
                        code = "server_error",
                        message = "An unexpected error occurred.",
                        traceId
                    }
                };
                var json = JsonSerializer.Serialize(payload);
                await context.Response.WriteAsync(json);
            }
        }
    }
}

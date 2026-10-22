using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WebApplication1.Middlerware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<RequestLoggingMiddleware> logger;
        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            await next(context);
            stopwatch.Stop();
            var level = context.Response.StatusCode >= 500 ? LogLevel.Error : context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            logger.Log(level, "HTTP {Method} {Path} returned {StatusCode} in {ElapsedMilliSeconds:F1} ms. TraceId: {TraceId}", context.Request.Method, context.Request.Path.Value, context.Response.StatusCode, stopwatch.Elapsed.TotalMilliseconds, context.TraceIdentifier);
        }
    }
}

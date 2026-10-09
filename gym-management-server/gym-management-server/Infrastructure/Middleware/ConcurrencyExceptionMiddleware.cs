using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Infrastructure.Middleware
{
    /// <summary>
    /// Centralised handling for EF Core optimistic-concurrency conflicts, so no controller needs
    /// its own try/catch for this: a conflicting update (stale RowVersion) returns 409 with a
    /// clear message instead of an unhandled 500.
    /// </summary>
    public class ConcurrencyExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ConcurrencyExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DbUpdateConcurrencyException)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Dữ liệu đã bị thay đổi bởi người khác, vui lòng tải lại và thử lại."
                });
            }
        }
    }
}

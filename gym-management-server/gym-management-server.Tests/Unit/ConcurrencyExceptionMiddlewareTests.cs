using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using gym_management_server.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class ConcurrencyExceptionMiddlewareTests
    {
        [Fact]
        public async Task Converts_DbUpdateConcurrencyException_into_409_with_a_message()
        {
            var middleware = new ConcurrencyExceptionMiddleware(_ => throw new DbUpdateConcurrencyException());

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var doc = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.True(doc.RootElement.TryGetProperty("message", out var message));
            Assert.False(string.IsNullOrWhiteSpace(message.GetString()));
        }

        [Fact]
        public async Task Lets_other_exceptions_propagate_unchanged()
        {
            var middleware = new ConcurrencyExceptionMiddleware(_ => throw new InvalidOperationException("boom"));
            var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

            await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        }
    }
}

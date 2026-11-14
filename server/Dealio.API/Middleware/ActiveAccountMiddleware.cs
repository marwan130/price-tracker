namespace PriceTracker.API.Middleware;

using Microsoft.EntityFrameworkCore;
using PriceTracker.API.Extensions;
using PriceTracker.Infrastructure.Persistence;

public sealed class ActiveAccountMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        var userId = context.User.GetUserIdOrNull();
        if (context.User.Identity?.IsAuthenticated == true && userId.HasValue)
        {
            var path = context.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
            var deletionRecoveryRoute = path.Equals("/v1/users/me", StringComparison.OrdinalIgnoreCase)
                && HttpMethods.IsGet(context.Request.Method)
                || path.Equals("/v1/users/me/deletion/cancel", StringComparison.OrdinalIgnoreCase)
                && HttpMethods.IsPost(context.Request.Method)
                || path.Equals("/v1/users/me/export", StringComparison.OrdinalIgnoreCase)
                && HttpMethods.IsGet(context.Request.Method);

            var active = await db.Users.AsNoTracking()
                .AnyAsync(x => x.UserId == userId && x.IsActive, context.RequestAborted);
            if (!active && !deletionRecoveryRoute)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    error = new { code = "ACCOUNT_INACTIVE", message = "This account is inactive or scheduled for deletion." }
                }, context.RequestAborted);
                return;
            }
        }

        await next(context);
    }
}

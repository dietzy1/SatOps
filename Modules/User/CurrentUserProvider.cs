namespace SatOps.Modules.User;

public interface ICurrentUserProvider
{
    int? GetUserId();
}

public class CurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public int? GetUserId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Get the internal database user ID that was added by ClaimsTransformer
        var userIdClaim = user.FindFirst("user_id");

        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }
}
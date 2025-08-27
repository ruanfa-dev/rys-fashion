using Microsoft.AspNetCore.Http;

using UseCases.Common.Security.Authentication.Contexts;

namespace Infrastructure.Security.Authentication.Contexts;

public sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid? UserId => _httpContextAccessor.HttpContext?.User.GetUserId();

    public string? UserName => _httpContextAccessor.HttpContext?.User.GetUserName();

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

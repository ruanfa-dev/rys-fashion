namespace Infrastructure.Security.Authorization.Providers;

public interface IUserAuthorizationProvider
{
    Task<UserAuthorizationData?> GetUserAuthorizationAsync(Guid userId);
    Task InvalidateUserAuthorizationAsync(Guid userId);
}
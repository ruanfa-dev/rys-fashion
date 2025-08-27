namespace UseCases.Common.Security.Authentication.Contexts;

public interface IUserContext
{
    Guid? UserId { get; } 
    string? UserName { get; }
    bool IsAuthenticated { get; }
}

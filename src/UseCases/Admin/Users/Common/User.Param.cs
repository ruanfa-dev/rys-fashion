namespace UseCases.Admin.Users.Common;

public record UserParam
{
    public required string Email { get; init; }
    public bool EmailConfirmed { get; set; } = false;
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; } = false;
    public string? ProfileImagePath { get; set; }
}

public record UserCreateParam : UserParam
{
    public required string Password { get; init; }
    public string[]? Roles { get; set; }
}
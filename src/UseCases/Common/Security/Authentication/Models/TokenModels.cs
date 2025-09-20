// ...existing code...

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Response model for external user information
/// </summary>
public sealed record ExternalUserInfo
{
    public string ProviderId { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string Username { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool EmailVerified { get; init; }
    public string? PictureUrl { get; init; }
}

/// <summary>
/// Request model for external token exchange
/// </summary>
public sealed record ExternalTokenExchangeRequest
{
    [Required]
    public string ExternalToken { get; init; } = string.Empty;
    
    [Required]
    public string Provider { get; init; } = string.Empty; // "google" or "facebook"
    
    public string? Scope { get; init; } = "openid profile email";
}

/// <summary>
/// Request model for external authentication
/// </summary>
public sealed record ExternalAuthRequest
{
    [Required]
    public string Provider { get; init; } = string.Empty; // "google" or "facebook"
    
    [Required]
    public string AccessToken { get; init; } = string.Empty;
    
    public string? IdToken { get; init; }
    
    public bool LinkToExistingUser { get; init; } = false;
    
    public string? ExistingUserId { get; init; }
}

/// <summary>
/// Response model for external authentication
/// </summary>
public sealed record ExternalAuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; }
    public UserInfo? UserInfo { get; init; }
    public bool IsNewUser { get; init; }
    public bool IsLinkedAccount { get; init; }
}
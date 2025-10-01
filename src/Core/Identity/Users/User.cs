using Core.Identity.Tokens;

using Microsoft.AspNetCore.Identity;

using SharedKernel.Domain.Attributes;
using SharedKernel.Domain.Attributes.Auditable;

namespace Core.Identity.Users;

public partial class User : IdentityUser<Guid>, IAuditable
{
    #region Properties

    #region Personal information
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    #endregion

    public string? ProfileImagePath { get; set; }

    #region Tracking
    public DateTimeOffset? LastSignInAt { get; set; }
    public string? LastSignInIp { get; set; }
    public DateTimeOffset? CurrentSignInAt { get; set; }
    public string? CurrentSignInIp { get; set; }
    public int SignInCount { get; set; } = 0;
    #endregion

    #region Auditable Properties
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    #endregion

    #region Relationships
    public ICollection<RefreshToken> RefreshTokens { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = null!;
    public ICollection<UserClaim> UserClaims { get; set; } = null!;
    #endregion

    #endregion

    #region Constructors
    protected User()
    {
        MarkAsCreated();
    }
    public static User Create(
        string email,
        bool emailConfirmed = false,
        string? userName = null,
        string? firstName = null,
        string? lastName = null,
        DateTimeOffset? dateOfBirth = null,
        string? profileImagePath = null,
        string? phoneNumber = null,
        bool phoneNumberConfirmed = false)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        // Fallback: if UserName is not provided, use email
        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = email;
        }

        User user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            EmailConfirmed = emailConfirmed,
            LockoutEnabled = true,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            ProfileImagePath = profileImagePath
        };

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            user.PhoneNumber = phoneNumber;
            user.PhoneNumberConfirmed = phoneNumberConfirmed;
        }

        return user;
    }
    #endregion

    #region Methods
    public User Update(
       string? email,
       bool emailConfirmed = false,
       string? userName = null,
       string? firstName = null,
       string? lastName = null,
       DateTimeOffset? dateOfBirth = null,
       string? profileImagePath = null,
       string? phoneNumber = null,
       bool phoneNumberConfirmed = false)
    {
        // Email: if provided, it must not be whitespace; update value and normalized form.
        if (email is not null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required when provided.", nameof(email));

            if (!string.Equals(Email, email, StringComparison.OrdinalIgnoreCase))
            {
                Email = email;
                NormalizedEmail = email.ToUpperInvariant();
            }

            EmailConfirmed = emailConfirmed;
        }

        // UserName: explicit value wins. If not provided but email was provided, fallback to email.
        if (!string.IsNullOrWhiteSpace(userName))
        {
            if (!string.Equals(UserName, userName, StringComparison.Ordinal))
            {
                UserName = userName;
                NormalizedUserName = userName.ToUpperInvariant();
            }
        }
        else if (email is not null) // fallback to email when updating email and no username provided
        {
            if (!string.Equals(UserName, email, StringComparison.Ordinal))
            {
                UserName = email;
                NormalizedUserName = email.ToUpperInvariant();
            }
        }

        // Personal information: update only when parameter is provided (null = no change).
        if (firstName is not null)
            FirstName = firstName;

        if (lastName is not null)
            LastName = lastName;

        if (dateOfBirth is not null)
            DateOfBirth = dateOfBirth;

        if (profileImagePath is not null)
            ProfileImagePath = profileImagePath;

        // Phone number: if provided (null = no change), update number and its confirmation state.
        if (phoneNumber is not null)
        {
            PhoneNumber = phoneNumber;
            PhoneNumberConfirmed = phoneNumberConfirmed;
        }

        return this;
    }

    #region Auditable Methods
    public void MarkAsCreated(string? userId = null)
    {
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = userId;
    }

    public void MarkAsUpdated(string? userId = null)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = userId;
    }
    #endregion

    #region Tracking
    public void RecordSignIn(string? ipAddress = null)
    {
        LastSignInAt = CurrentSignInAt;
        LastSignInIp = CurrentSignInIp;
        CurrentSignInAt = DateTimeOffset.UtcNow;
        CurrentSignInIp = ipAddress;
        SignInCount++;
    }

    #endregion
    #endregion
}

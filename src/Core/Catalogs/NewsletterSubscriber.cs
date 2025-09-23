using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

using Core.Identity;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Lightweight domain port of Spree::NewsletterSubscriber.
/// Persistence (uniqueness, background emails) and application workflows (Subscribe/Verify services)
/// belong to application/infrastructure layers. This class contains core behaviour and validation helpers.
/// </summary>
public sealed class NewsletterSubscriber : AuditableEntity
{
    public string Email { get; private set; } = default!;
    public Guid? UserId { get; private set; }
    public User? User { get; private set; }

    public string VerificationToken { get; private set; } = string.Empty;
    public DateTimeOffset? VerifiedAt { get; private set; }

    private NewsletterSubscriber() { }

    public static NewsletterSubscriber Create(string email, Guid? userId = null)
    {
        var s = new NewsletterSubscriber
        {
            Email = (email ?? string.Empty).Trim(),
            UserId = userId
        };
        s.EnsureVerificationToken();
        s.AddDomainEvent(new Events.Created(s.Id));
        return s;
    }

    public bool Verified() => VerifiedAt.HasValue;

    public void EnsureVerificationToken()
    {
        if (!string.IsNullOrWhiteSpace(VerificationToken)) return;
        VerificationToken = GenerateSecureToken();
        MarkAsUpdated();
    }

    public bool Verify(string token)
    {
        if (Verified()) return false;
        if (string.IsNullOrWhiteSpace(token)) return false;
        if (!string.Equals(VerificationToken, token, StringComparison.Ordinal)) return false;

        VerifiedAt = DateTimeOffset.UtcNow;
        MarkAsUpdated();
        AddDomainEvent(new Events.Verified(Id));
        return true;
    }

    public string ToCsv()
    {
        var verified = VerifiedAt.HasValue ? VerifiedAt.Value.UtcDateTime.ToString("o") : string.Empty;
        // simple CSV: email,verified_at
        return $"{EscapeForCsv(Email)},{EscapeForCsv(verified)}";
    }

    private static string EscapeForCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string GenerateSecureToken(int bytes = 16)
    {
        var buffer = new byte[bytes];
        RandomNumberGenerator.Fill(buffer);
        var sb = new StringBuilder(buffer.Length * 2);
        foreach (var b in buffer) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int EmailMaxLength = 255;
    }

    public static class Errors
    {
        public static Error EmailRequired => Error.Validation("NewsletterSubscriber.EmailRequired", "Email is required.");
        public static Error EmailInvalid => Error.Validation("NewsletterSubscriber.EmailInvalid", "Email is invalid.");
        public static Error NotFound(Guid id) => Error.NotFound("NewsletterSubscriber.NotFound", $"NewsletterSubscriber with ID '{id}' was not found.");
    }

    /// <summary>
    /// Lightweight validation for handlers. Uniqueness must be enforced by repository/infrastructure.
    /// </summary>
    public static List<Error> ValidateModel(string? email)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(Errors.EmailRequired);
            return errors;
        }

        var trimmed = email!.Trim();
        if (trimmed.Length > Constraints.EmailMaxLength)
        {
            errors.Add(Errors.EmailInvalid);
            return errors;
        }

        // best-effort RFC check
        try
        {
            var _ = new MailAddress(trimmed);
        }
        catch
        {
            errors.Add(Errors.EmailInvalid);
        }

        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid NewsletterSubscriberId) : DomainEvent;
        public record Verified(Guid NewsletterSubscriberId) : DomainEvent;
        public record Deleted(Guid NewsletterSubscriberId) : DomainEvent;
    }

    #endregion
}
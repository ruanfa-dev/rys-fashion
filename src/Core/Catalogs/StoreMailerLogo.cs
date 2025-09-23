using System;
using System.Collections.Generic;
using System.Linq;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain representation of a store mailer logo asset (port of Spree::StoreMailerLogo).
/// - Content-type restrictions are expressed here as lightweight validation helpers.
/// - Actual attachment storage/validation should be enforced by infra (file storage / content-type checks).
/// </summary>
public sealed class StoreMailerLogo : Asset
{
    public static readonly string[] VALID_CONTENT_TYPES = { "image/png", "image/jpg", "image/jpeg" };

    private StoreMailerLogo() { }

    public static StoreMailerLogo Create(Guid ownerId, string? attachmentKey, string? contentType = null)
    {
        return new StoreMailerLogo
        {
            AttachmentKey = string.IsNullOrWhiteSpace(attachmentKey) ? null : attachmentKey!.Trim(),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType!.Trim()
        };
    }

    public bool ContentTypeValid()
        => !string.IsNullOrWhiteSpace(ContentType) && VALID_CONTENT_TYPES.Contains(ContentType, StringComparer.OrdinalIgnoreCase);

    public static List<Error> ValidateModel(string? attachmentKey, string? contentType)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(attachmentKey))
            errors.Add(Error.Validation("StoreMailerLogo.AttachmentRequired", "Attachment key is required."));
        if (!string.IsNullOrWhiteSpace(contentType) && !VALID_CONTENT_TYPES.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            errors.Add(Error.Validation("StoreMailerLogo.InvalidContentType", $"Content type must be one of: {string.Join(", ", VALID_CONTENT_TYPES)}."));
        return errors;
    }

    public static class Events
    {
        public record Created(Guid StoreMailerLogoId) : DomainEvent;
        public record Updated(Guid StoreMailerLogoId) : DomainEvent;
        public record Deleted(Guid StoreMailerLogoId) : DomainEvent;
    }
}
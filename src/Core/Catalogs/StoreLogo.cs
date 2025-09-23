using System;
using System.Collections.Generic;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain representation of a store logo asset (port of Spree::StoreLogo).
/// - Lightweight wrapper around Asset to provide a clear type for store logos.
/// - Attachment storage and validation belong in infrastructure.
/// </summary>
public sealed class StoreLogo : Asset
{
    private StoreLogo() { }

    public static StoreLogo Create(Guid ownerId, string? attachmentKey, string? contentType = null)
    {
        return new StoreLogo
        {
            AttachmentKey = string.IsNullOrWhiteSpace(attachmentKey) ? null : attachmentKey!.Trim(),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType!.Trim()
        };
    }

    public static List<Error> ValidateModel(string? attachmentKey)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(attachmentKey))
            errors.Add(Error.Validation("StoreLogo.AttachmentRequired", "Attachment key is required."));
        return errors;
    }

    public static class Events
    {
        public record Created(Guid StoreLogoId) : DomainEvent;
        public record Updated(Guid StoreLogoId) : DomainEvent;
        public record Deleted(Guid StoreLogoId) : DomainEvent;
    }
}
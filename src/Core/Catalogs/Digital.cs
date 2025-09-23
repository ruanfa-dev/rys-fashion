using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;

namespace Core.Catalogs;

/// <summary>
/// Domain model port of Spree::Digital (simplified).
/// - Attachment binary/storage is an infrastructure concern; the domain keeps lightweight attachment metadata (key, filename, content type).
/// - DigitalLinks (delivery links) are modeled as navigation; infra/service layer is responsible for generating and persisting them.
/// </summary>
public sealed class Digital : AuditableEntity
{
    public Guid VariantId { get; set; }
    public Variant? Variant { get; set; }

    // Attachment metadata (actual blob handling belongs to infra)
    public string? AttachmentKey { get; private set; }
    public string? FileName { get; private set; }
    public string? ContentType { get; private set; }

    public virtual ICollection<DigitalLink> DigitalLinks { get; set; } = new List<DigitalLink>();

    private Digital() { }

    public static Digital Create(Guid variantId, string attachmentKey, string? fileName = null, string? contentType = null)
    {
        var d = new Digital
        {
            VariantId = variantId
        };
        d.SetAttachment(attachmentKey, fileName, contentType);
        return d;
    }

    public void SetAttachment(string attachmentKey, string? fileName = null, string? contentType = null)
    {
        AttachmentKey = string.IsNullOrWhiteSpace(attachmentKey) ? null : attachmentKey.Trim();
        FileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim();
        ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
        MarkAsUpdated();
    }

    public void RemoveAttachment()
    {
        AttachmentKey = null;
        FileName = null;
        ContentType = null;
        MarkAsUpdated();
    }

    // Delegate convenience
    public Product? Product => Variant?.Product;

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int FileNameMaxLength = 255;
    }

    public static class Errors
    {
        public static Error VariantRequired => Error.Validation("Digital.VariantRequired", "Variant is required.");
        public static Error AttachmentRequired => Error.Validation("Digital.AttachmentRequired", "Attachment is required.");
        public static Error FileNameTooLong => Error.Validation("Digital.FileNameTooLong", $"File name must be at most {Constraints.FileNameMaxLength} characters.");
        public static Error NotFound(Guid id) => Error.NotFound("Digital.NotFound", $"Digital with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(Guid variantId, string? attachmentKey, string? fileName = null)
    {
        var errors = new List<Error>();
        if (variantId == Guid.Empty) errors.Add(Errors.VariantRequired);
        if (string.IsNullOrWhiteSpace(attachmentKey)) errors.Add(Errors.AttachmentRequired);
        if (!string.IsNullOrWhiteSpace(fileName) && fileName!.Length > Constraints.FileNameMaxLength) errors.Add(Errors.FileNameTooLong);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid DigitalId) : DomainEvent;
        public record Updated(Guid DigitalId) : DomainEvent;
        public record Deleted(Guid DigitalId) : DomainEvent;
    }

    #endregion
}

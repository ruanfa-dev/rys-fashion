using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;

namespace Core.Catalogs;

/// <summary>
/// Domain representation of a Taxon-specific image (port of Spree::TaxonImage).
/// Inherits Image and links to a Taxon. URL generation is delegated to infra via the provided urlFactory.
/// </summary>
public sealed class TaxonImage : Image
{
    public Guid TaxonId { get; set; }
    public Taxon? Taxon { get; set; }

    private TaxonImage() { }

    public static TaxonImage Create(Guid taxonId, string? attachmentKey = null)
    {
        return new TaxonImage
        {
            TaxonId = taxonId,
            AttachmentKey = attachmentKey
        };
    }

    /// <summary>
    /// Returns style descriptors for this taxon image. Caller supplies urlFactory(attachmentKey, styleSize).
    /// </summary>
    public IEnumerable<(string Name, string Url, string Size, int Width, int Height)> Styles(Func<string?, string, string> urlFactory)
        => StylesWithUrls(urlFactory);

    /// <summary>
    /// Return a single style descriptor for this taxon image.
    /// </summary>
    public (string Name, string Url, string Size, int Width, int Height)? Style(string name, Func<string?, string, string> urlFactory)
        => base.Style(name, urlFactory);

    #region Validation / Errors / Events

    public static class Errors
    {
        public static Error TaxonRequired => Error.Validation("TaxonImage.TaxonRequired", "Taxon is required.");
        public static Error AttachmentRequired => Error.Validation("TaxonImage.AttachmentRequired", "Attachment key is required for an image.");
        public static Error NotFound(Guid id) => Error.NotFound("TaxonImage.NotFound", $"TaxonImage with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(Guid taxonId, string? attachmentKey)
    {
        var errors = new List<Error>();
        if (taxonId == Guid.Empty) errors.Add(Errors.TaxonRequired);
        if (string.IsNullOrWhiteSpace(attachmentKey)) errors.Add(Errors.AttachmentRequired);
        return errors;
    }

    public static class Events
    {
        public record Created(Guid TaxonImageId) : DomainEvent;
        public record Updated(Guid TaxonImageId) : DomainEvent;
        public record Deleted(Guid TaxonImageId) : DomainEvent;
    }

    #endregion
}
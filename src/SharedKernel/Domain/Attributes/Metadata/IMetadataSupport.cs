namespace SharedKernel.Domain.Attributes.Metadata;

/// <summary>
/// Marker interface for entities that persist JSON-backed metadata columns.
/// Implement on domain entities to get the MetadataSupport extension helpers.
/// EF Core: Map these properties with ValueJsonConverter for JSON column support.
/// </summary>
public interface IMetadataSupport
{
    /// <summary>
    /// Dictionary property storing public metadata (maps to JSON/JSONB column in the DB).
    /// </summary>
    IDictionary<string, string?>? PublicMetadata { get; set; }

    /// <summary>
    /// Dictionary property storing private metadata (maps to JSON/JSONB column in the DB).
    /// </summary>
    IDictionary<string, string?>? PrivateMetadata { get; set; }
}

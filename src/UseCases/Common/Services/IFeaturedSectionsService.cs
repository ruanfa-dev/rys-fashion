namespace UseCases.Common.Services;

public interface IFeaturedSectionsService
{
    /// <summary>
    /// Touch (update timestamp) featured sections for the provided taxon ids.
    /// </summary>
    Task TouchFeaturedSectionsAsync(IEnumerable<Guid> taxonIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove featured sections entries for the provided taxon ids.
    /// </summary>
    Task RemoveFeaturedSectionsAsync(IEnumerable<Guid> taxonIds, CancellationToken cancellationToken = default);
}

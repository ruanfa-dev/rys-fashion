using System.Reflection;

using Infrastructure.Persistence.Converters;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Domain.Attributes.Metadata;

namespace Infrastructure.Persistence.Configurations.Common;

internal static class MetadataSupportModelBuilderExtensions
{
    public static void ApplyMetadataSupportConversions(this ModelBuilder builder)
    {
        var metadataInterface = typeof(IMetadataSupport);
        var converter = DictionaryJsonConverter.GetConverter();
        var comparer = DictionaryJsonConverter.GetComparer();

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType == null) continue;
            if (!metadataInterface.IsAssignableFrom(clrType)) continue;

            var entityBuilder = builder.Entity(clrType);

            // PublicMetadata
            var pubProp = clrType.GetProperty(nameof(IMetadataSupport.PublicMetadata), BindingFlags.Public | BindingFlags.Instance);
            if (pubProp != null)
            {
                entityBuilder
                    .Property(typeof(IDictionary<string, string?>), pubProp.Name)
                    .HasConversion(converter);
                entityBuilder
                    .Property(typeof(IDictionary<string, string?>), pubProp.Name)
                    .Metadata.SetValueComparer(comparer);
            }

            // PrivateMetadata
            var privProp = clrType.GetProperty(nameof(IMetadataSupport.PrivateMetadata), BindingFlags.Public | BindingFlags.Instance);
            if (privProp != null)
            {
                entityBuilder
                    .Property(typeof(IDictionary<string, string?>), privProp.Name)
                    .HasConversion(converter);
                entityBuilder
                    .Property(typeof(IDictionary<string, string?>), privProp.Name)
                    .Metadata.SetValueComparer(comparer);
            }
        }
    }
}
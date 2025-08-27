using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Converters;

public static class UtcDateAnnotation
{
    private const string IsUtcAnnotation = "IsUtc";

    // For DateTime: Always ensure UTC kind when storing and retrieving
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter =
        new ValueConverter<DateTime, DateTime>(
            // To database: ensure UTC
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            // From database: always mark as UTC
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> UtcNullableDateTimeConverter =
        new ValueConverter<DateTime?, DateTime?>(
            // To database: ensure UTC
            v => v.HasValue ? v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime() : null,
            // From database: always mark as UTC
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

    // For DateTimeOffset: Standardize on UTC during storage/retrieval
    private static readonly ValueConverter<DateTimeOffset, DateTimeOffset> UtcDateTimeOffsetConverter =
        new ValueConverter<DateTimeOffset, DateTimeOffset>(
            // To database: normalize to UTC
            v => v.ToUniversalTime(),
            // From database: ensure offset is UTC+0
            v => v.Offset == TimeSpan.Zero ? v : new DateTimeOffset(v.UtcDateTime, TimeSpan.Zero));

    private static readonly ValueConverter<DateTimeOffset?, DateTimeOffset?> UtcNullableDateTimeOffsetConverter =
        new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
            // To database: normalize to UTC
            v => v.HasValue ? v.Value.ToUniversalTime() : null,
            // From database: ensure offset is UTC+0
            v => v.HasValue ? v.Value.Offset == TimeSpan.Zero ? v.Value : new DateTimeOffset(v.Value.UtcDateTime, TimeSpan.Zero) : null);

    public static PropertyBuilder<TProperty> IsUtc<TProperty>(this PropertyBuilder<TProperty> builder, bool isUtc = true)
    {
        builder.HasAnnotation(IsUtcAnnotation, isUtc);
        return builder;
    }

    public static bool IsUtc(this IMutableProperty property)
    {
        var annotation = property.FindAnnotation(IsUtcAnnotation);
        return annotation == null || (bool?)annotation.Value != false;
    }

    public static void ApplyUtcDateTimeConverter(this ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (!property.IsUtc())
                    continue;

                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(UtcDateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(UtcNullableDateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(UtcDateTimeOffsetConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(UtcNullableDateTimeOffsetConverter);
                }
            }
        }
    }
}

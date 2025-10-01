using System.Reflection;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Domain.Attributes.Positionable;

namespace Infrastructure.Persistence.Configurations.Common.Attributes;

internal static class PositionableModelBuilderExtensions
{
    public static void ApplyPositionableConversions(this ModelBuilder builder)
    {
        Type positionableInterface = typeof(IPositionable);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!positionableInterface.IsAssignableFrom(clrType)) continue;

            var entityBuilder = builder.Entity(clrType);

            var posProp = clrType.GetProperty(nameof(IPositionable.Position), BindingFlags.Public | BindingFlags.Instance);
            if (posProp != null)
            {
                entityBuilder
                    .Property(typeof(int), posProp.Name)
                    .HasDefaultValue(PositionableConstraints.PositionMin);
            }
        }
    }
}
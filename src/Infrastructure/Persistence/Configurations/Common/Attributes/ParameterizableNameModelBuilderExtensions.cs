using System.Reflection;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Domain.Attributes.Parameterizable;

namespace Infrastructure.Persistence.Configurations.Common.Attributes;

internal static class ParameterizableNameModelBuilderExtensions
{
    public static void ApplyParameterizableNameConversions(this ModelBuilder builder)
    {
        Type parameterizableInterface = typeof(IParameterizableName);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType == null) continue;
            if (!parameterizableInterface.IsAssignableFrom(clrType)) continue;

            var entityBuilder = builder.Entity(clrType);

            // Configure Name property (required, limited length)
            var nameProp = clrType.GetProperty(nameof(IParameterizableName.Name), BindingFlags.Public | BindingFlags.Instance);
            if (nameProp != null)
            {
                entityBuilder
                    .Property(typeof(string), nameProp.Name)
                    .IsRequired()
                    .HasMaxLength(ParameterizableConstraints.NameMaxLength);
            }

            // Configure Presentation property (required, limited length)
            var presProp = clrType.GetProperty(nameof(IParameterizableName.Presentation), BindingFlags.Public | BindingFlags.Instance);
            if (presProp != null)
            {
                entityBuilder
                    .Property(typeof(string), presProp.Name)
                    .IsRequired()
                    .HasMaxLength(ParameterizableConstraints.PresentationMaxLength);
            }
        }
    }
}
using System.Reflection;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Domain.Attributes.TranslatableResource;

namespace Infrastructure.Persistence.Configurations.Common;

/// <summary>
/// Apply generic translation-related IEntityTypeConfiguration implementations for all matching CLR types
/// in the model. EF's ApplyConfigurationsFromAssembly does not pick up open-generic configurations,
/// so these helpers close the generic types and apply them for each discovered concrete type.
/// Call from ApplicationDbContext.OnModelCreating(builder).
/// </summary>
internal static class TranslationModelBuilderExtensions
{
    public static void ApplyTranslationEntityConfigurations(this ModelBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        // Apply TTranslation : ITranslation => TranslationEntityConfiguration<TTranslation>
        ApplyGenericConfigurationForInterface(builder, typeof(ITranslation), typeof(TranslationEntityConfiguration<>));

        // Apply TEntity : ITranslatable<TTranslation> => TranslatableEntityConfiguration<TEntity, TTranslation>
        // We look for types implementing ITranslatable<...> and create a closed TranslatableEntityConfiguration<TEntity, TTranslation>
        var translatableInterfaceDef = typeof(ITranslatable<>);
        var translatableConfigDef = typeof(TranslatableEntityConfiguration<,>);

        var modelEntityTypes = builder.Model.GetEntityTypes().Select(et => et.ClrType).Where(t => t != null).ToArray();

        foreach (var entityType in modelEntityTypes)
        {
            var translatableIfaces = entityType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == translatableInterfaceDef)
                .ToArray();

            if (translatableIfaces.Length == 0) continue;

            // For each ITranslatable<TTranslation> implemented by the entity, create and apply a config instance
            foreach (var iface in translatableIfaces)
            {
                var translationType = iface.GetGenericArguments()[0];
                var closedConfigType = translatableConfigDef.MakeGenericType(entityType, translationType);
                var configInstance = Activator.CreateInstance(closedConfigType);
                if (configInstance == null) continue;

                var applyMethod = typeof(ModelBuilder).GetMethod(nameof(ModelBuilder.ApplyConfiguration), BindingFlags.Instance | BindingFlags.Public);
                if (applyMethod == null) continue;

                var genericApply = applyMethod.MakeGenericMethod(entityType);
                genericApply.Invoke(builder, new[] { configInstance! });
            }
        }
    }

    private static void ApplyGenericConfigurationForInterface(ModelBuilder builder, Type markerInterface, Type genericConfigDef)
    {
        var applyMethod = typeof(ModelBuilder).GetMethod(nameof(ModelBuilder.ApplyConfiguration), BindingFlags.Instance | BindingFlags.Public);
        if (applyMethod == null) return;

        var modelEntityTypes = builder.Model.GetEntityTypes().Select(et => et.ClrType).Where(t => t != null).ToArray();

        foreach (var clrType in modelEntityTypes)
        {
            // translation entity types implement markerInterface directly
            if (!markerInterface.IsAssignableFrom(clrType)) continue;

            var closedConfigType = genericConfigDef.MakeGenericType(clrType);
            var configInstance = Activator.CreateInstance(closedConfigType);
            if (configInstance == null) continue;

            var genericApply = applyMethod.MakeGenericMethod(clrType);
            genericApply.Invoke(builder, new[] { configInstance! });
        }
    }
}

using System.Reflection;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Domain.Attributes.TranslatableResource;

namespace Infrastructure.Persistence.Configurations.Common.Attributes;

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
        Type translatableInterfaceDef = typeof(ITranslatable<>);
        Type translatableConfigDef = typeof(TranslatableEntityConfiguration<,>);

        Type[] modelEntityTypes = builder.Model.GetEntityTypes().Select(et => et.ClrType).Where(t => t != null).ToArray();

        foreach (Type entityType in modelEntityTypes)
        {
            Type[] translatableIfaces = entityType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == translatableInterfaceDef)
                .ToArray();

            if (translatableIfaces.Length == 0) continue;

            // For each ITranslatable<TTranslation> implemented by the entity, create and apply a config instance
            foreach (Type iface in translatableIfaces)
            {
                Type translationType = iface.GetGenericArguments()[0];
                Type closedConfigType = translatableConfigDef.MakeGenericType(entityType, translationType);
                object? configInstance = Activator.CreateInstance(closedConfigType);
                if (configInstance == null) continue;

                MethodInfo? applyMethod = typeof(ModelBuilder).GetMethod(nameof(ModelBuilder.ApplyConfiguration), BindingFlags.Instance | BindingFlags.Public);
                if (applyMethod == null) continue;

                MethodInfo genericApply = applyMethod.MakeGenericMethod(entityType);
                genericApply.Invoke(builder, [configInstance!]);
            }
        }
    }

    private static void ApplyGenericConfigurationForInterface(ModelBuilder builder, Type markerInterface, Type genericConfigDef)
    {
        MethodInfo? applyMethod = typeof(ModelBuilder).GetMethod(nameof(ModelBuilder.ApplyConfiguration), BindingFlags.Instance | BindingFlags.Public);
        if (applyMethod == null) return;

        Type[] modelEntityTypes = builder.Model.GetEntityTypes().Select(et => et.ClrType).Where(t => t != null).ToArray();

        foreach (Type clrType in modelEntityTypes)
        {
            // translation entity types implement markerInterface directly
            if (!markerInterface.IsAssignableFrom(clrType)) continue;

            Type closedConfigType = genericConfigDef.MakeGenericType(clrType);
            object? configInstance = Activator.CreateInstance(closedConfigType);
            if (configInstance == null) continue;

            MethodInfo genericApply = applyMethod.MakeGenericMethod(clrType);
            genericApply.Invoke(builder, [configInstance!]);
        }
    }
}

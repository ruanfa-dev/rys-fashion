using System.Reflection;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Domain.Attributes.TranslatableResource;

namespace Infrastructure.Persistence.Configurations.Common.Attributes;

/// <summary>
/// Apply open-generic translation-related configurations for all matching CLR types
/// in the model. EF's ApplyConfigurationsFromAssembly does not discover open-generic
/// IEntityTypeConfiguration implementations, so close and apply them here.
/// Call from ApplicationDbContext.OnModelCreating after ApplyConfigurationsFromAssembly.
/// </summary>
internal static class TranslatableModelBuilderExtensions
{
    public static void ApplyTranslatableConfigurations(this ModelBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        Type[] modelEntityTypes = builder.Model.GetEntityTypes()
            .Select(et => et.ClrType)
            .Where(t => t != null)
            .ToArray();

        MethodInfo? applyMethod = typeof(ModelBuilder).GetMethod(nameof(ModelBuilder.ApplyConfiguration), BindingFlags.Instance | BindingFlags.Public);
        if (applyMethod == null) return;

        // 1) For every translation entity type (implements ITranslation), apply TranslationEntityConfiguration<TTranslation>
        Type translationConfigDef = typeof(TranslationEntityConfiguration<>);
        Type translationMarker = typeof(ITranslation);
        foreach (Type clr in modelEntityTypes)
        {
            if (translationMarker.IsAssignableFrom(clr))
            {
                Type closed = translationConfigDef.MakeGenericType(clr);
                object? instance = Activator.CreateInstance(closed);
                MethodInfo genericApply = applyMethod.MakeGenericMethod(clr);
                genericApply.Invoke(builder, [instance!]);
            }
        }

        // 2) For every entity implementing ITranslatable<TTranslation>, apply TranslatableEntityConfiguration<TEntity, TTranslation>
        Type translatableInterfaceDef = typeof(ITranslatable<>);
        Type translatableConfigDef = typeof(TranslatableEntityConfiguration<,>);

        foreach (Type entityClr in modelEntityTypes)
        {
            Type[] translatableIfaces = entityClr.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == translatableInterfaceDef)
                .ToArray();

            if (translatableIfaces.Length == 0) continue;

            foreach (Type iface in translatableIfaces)
            {
                Type translationType = iface.GetGenericArguments()[0];
                Type closedConfigType = translatableConfigDef.MakeGenericType(entityClr, translationType);
                object? configInstance = Activator.CreateInstance(closedConfigType);
                if (configInstance == null) continue;

                MethodInfo genericApply = applyMethod.MakeGenericMethod(entityClr);
                genericApply.Invoke(builder, [configInstance!]);
            }
        }
    }
}
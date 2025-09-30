using System.Reflection;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Domain.Attributes.TranslatableResource;

namespace Infrastructure.Persistence.Configurations.Common;

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

        var modelEntityTypes = builder.Model.GetEntityTypes()
            .Select(et => et.ClrType)
            .Where(t => t != null)
            .ToArray();

        var applyMethod = typeof(ModelBuilder).GetMethod(nameof(ModelBuilder.ApplyConfiguration), BindingFlags.Instance | BindingFlags.Public);
        if (applyMethod == null) return;

        // 1) For every translation entity type (implements ITranslation), apply TranslationEntityConfiguration<TTranslation>
        var translationConfigDef = typeof(TranslationEntityConfiguration<>);
        var translationMarker = typeof(ITranslation);
        foreach (var clr in modelEntityTypes)
        {
            if (translationMarker.IsAssignableFrom(clr))
            {
                var closed = translationConfigDef.MakeGenericType(clr);
                var instance = Activator.CreateInstance(closed);
                var genericApply = applyMethod.MakeGenericMethod(clr);
                genericApply.Invoke(builder, new[] { instance! });
            }
        }

        // 2) For every entity implementing ITranslatable<TTranslation>, apply TranslatableEntityConfiguration<TEntity, TTranslation>
        var translatableInterfaceDef = typeof(ITranslatable<>);
        var translatableConfigDef = typeof(TranslatableEntityConfiguration<,>);

        foreach (var entityClr in modelEntityTypes)
        {
            var translatableIfaces = entityClr.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == translatableInterfaceDef)
                .ToArray();

            if (translatableIfaces.Length == 0) continue;

            foreach (var iface in translatableIfaces)
            {
                var translationType = iface.GetGenericArguments()[0];
                var closedConfigType = translatableConfigDef.MakeGenericType(entityClr, translationType);
                var configInstance = Activator.CreateInstance(closedConfigType);
                if (configInstance == null) continue;

                var genericApply = applyMethod.MakeGenericMethod(entityClr);
                genericApply.Invoke(builder, new[] { configInstance! });
            }
        }
    }
}
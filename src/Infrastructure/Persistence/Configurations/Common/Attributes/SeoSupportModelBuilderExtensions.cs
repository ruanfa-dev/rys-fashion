using System.Reflection;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Domain.Attributes.Seo;

namespace Infrastructure.Persistence.Configurations.Common.Attributes;

internal static class SeoSupportModelBuilderExtensions
{
    public static void ApplySeoSupportConversions(this ModelBuilder builder)
    {
        Type seoSupportInterface = typeof(ISeoSupport);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!seoSupportInterface.IsAssignableFrom(clrType)) continue;

            var entityBuilder = builder.Entity(clrType);

            // MetaTitle
            var metaTitleProp = clrType.GetProperty(nameof(ISeoSupport.MetaTitle), BindingFlags.Public | BindingFlags.Instance);
            if (metaTitleProp != null)
            {
                entityBuilder
                    .Property(typeof(string), metaTitleProp.Name)
                    .HasMaxLength(SeoSupportConstraints.MetaTitleMaxLength)
                    .IsRequired(false);
            }

            // MetaDescription
            var metaDescriptionProp = clrType.GetProperty(nameof(ISeoSupport.MetaDescription), BindingFlags.Public | BindingFlags.Instance);
            if (metaDescriptionProp != null)
            {
                entityBuilder
                    .Property(typeof(string), metaDescriptionProp.Name)
                    .HasMaxLength(SeoSupportConstraints.MetaDescriptionMaxLength)
                    .IsRequired(false);
            }

            // MetaKeywords
            var metaKeywordsProp = clrType.GetProperty(nameof(ISeoSupport.MetaKeywords), BindingFlags.Public | BindingFlags.Instance);
            if (metaKeywordsProp != null)
            {
                entityBuilder
                    .Property(typeof(string), metaKeywordsProp.Name)
                    .HasMaxLength(SeoSupportConstraints.MetaFieldMaxLength)
                    .IsRequired(false);
            }
        }
    }
}
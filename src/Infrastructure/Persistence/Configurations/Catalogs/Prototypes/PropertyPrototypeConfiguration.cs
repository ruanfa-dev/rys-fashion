
using Core.Catalog.Prototypes;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Prototypes;
public class PropertyPrototypeConfiguration : IEntityTypeConfiguration<PropertyPrototype>
{
	public void Configure(EntityTypeBuilder<PropertyPrototype> builder)
	{
		// Table name
		builder.ToTable(Schema.PrototypeProperties);

		// Primary key
		builder.HasKey(pp => pp.Id);
		builder.HasIndex(pp => new { pp.PrototypeId, pp.PropertyId }).IsUnique();

		// Relationships
		builder.HasOne(pp => pp.Prototype)
			.WithMany(p => p.PropertyPrototypes)
			.HasForeignKey(pp => pp.PrototypeId)
			.OnDelete(DeleteBehavior.Cascade);
		builder.HasOne(pp => pp.Property)
			.WithMany(p => p.PrototypeProperties)
			.HasForeignKey(pp => pp.PropertyId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}

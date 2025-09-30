
using Core.Catalog.Prototypes;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Prototypes;
public sealed class PrototypeConfiguration : IEntityTypeConfiguration<Prototype>
{
	public void Configure(EntityTypeBuilder<Prototype> builder)
	{
		// Table name
		builder.ToTable(Schema.Prototypes);

		// Primary key
		builder.HasKey(p => p.Id);

		// Properties
		builder.Property(p => p.Name)
			.HasMaxLength(Prototype.Constraints.NameMaxLength)
			.IsRequired();
		builder.Property(p => p.Description)
			.HasMaxLength(Prototype.Constraints.DescriptionMaxLength);
		builder.Property(p => p.Position)
			.IsRequired();

		// Relationships
		builder.HasMany(p => p.PropertyPrototypes)
			.WithOne(pp => pp.Prototype)
			.HasForeignKey(pp => pp.PrototypeId);

		// Indexes
		builder.HasIndex(p => p.Name);
		builder.HasIndex(p => p.Position);
	}
}

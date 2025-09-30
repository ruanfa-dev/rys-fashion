
using Core.Catalog.Prototypes;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Prototypes;
public class PrototypeTaxonConfiguration : IEntityTypeConfiguration<PrototypeTaxon>
{
	public void Configure(EntityTypeBuilder<PrototypeTaxon> builder)
	{
		builder.ToTable(Schema.PrototypeTaxons);

		builder.HasKey(pt => pt.Id);
		builder.HasIndex(pt => new { pt.PrototypeId, pt.TaxonId }).IsUnique();

		builder.HasOne(pt => pt.Prototype)
			.WithMany(p => p.PrototypeTaxons)
			.HasForeignKey(pt => pt.PrototypeId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(pt => pt.Taxon)
			.WithMany(t => t.PrototypeTaxons)
			.HasForeignKey(pt => pt.TaxonId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}

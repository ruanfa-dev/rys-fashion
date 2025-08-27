using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Core.Todos;

using Infrastructure.Persistence.Constants;
using Infrastructure.Persistence.Converters;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Todos;
internal sealed class TodoListConfiguration : IEntityTypeConfiguration<TodoList>
{
    public void Configure(EntityTypeBuilder<TodoList> builder)
    {
        builder.ToTable(Schema.TodoLists);

        // Configure primary key
        builder.HasKey(t => t.Id);

        // Configure properties
        builder.Property(t => t.Title)
            .HasMaxLength(TodoList.Constraints.MaxTitleLength)
            .IsRequired(false);

        builder.Property(t => t.Colour)
            .HasValueJsonConverter()
            .HasColumnType("TEXT");

        builder.HasMany(t => t.Items)
            .WithOne(m => m.List)
            .HasForeignKey(ti => ti.ListId);
    }
}

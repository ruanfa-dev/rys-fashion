using Core.Todos;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Todos;
internal sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable(Schema.TodoItems);

        // Configure primary key (inherited from BaseAuditableEntity)
        builder.HasKey(ti => ti.Id);

        // Configure properties
        builder.Property(ti => ti.Title)
            .IsRequired()
            .HasMaxLength(TodoItem.Constraints.MaxTitleLength);

        builder.Property(ti => ti.Note)
            .HasMaxLength(TodoItem.Constraints.MaxNoteLength);

        builder.Property(ti => ti.Priority)
            .HasConversion<int>();

        builder.Property(ti => ti.Done)
            .IsRequired();

        builder.Property(ti => ti.DoneAt)
            .IsRequired(false);

        // Configure relationships
        builder.HasOne(ti => ti.List)
            .WithMany(m => m.Items)
            .HasForeignKey(ti => ti.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optionally: Configure indexes
        builder.HasIndex(ti => ti.ListId);
    }
}
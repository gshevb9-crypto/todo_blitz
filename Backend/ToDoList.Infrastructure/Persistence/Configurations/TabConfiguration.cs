using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToDoList.Domain.Entities;

namespace ToDoList.Infrastructure.Persistence.Configurations
{
    public class TabConfiguration : IEntityTypeConfiguration<Tab>
    {
        public void Configure(EntityTypeBuilder<Tab> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Icon)
                .HasMaxLength(50);

            builder.Property(t => t.UserId)
                .IsRequired();

            builder.HasIndex(t => new { t.UserId, t.IsDeleted });
            builder.HasIndex(t => new { t.UserId, t.OrderIndex });

            builder.HasOne(t => t.User)
                .WithMany(u => u.Tabs)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.Content)
                .WithOne(c => c.Tab)
                .HasForeignKey<TabContent>(c => c.TabId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Images)
                .WithOne(i => i.Tab)
                .HasForeignKey(i => i.TabId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

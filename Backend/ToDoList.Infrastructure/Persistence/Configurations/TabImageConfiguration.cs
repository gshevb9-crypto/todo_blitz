using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToDoList.Domain.Entities;

namespace ToDoList.Infrastructure.Persistence.Configurations
{
    public class TabImageConfiguration : IEntityTypeConfiguration<TabImage>
    {
        public void Configure(EntityTypeBuilder<TabImage> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.FileName)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(i => i.OriginalFileName)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(i => i.RelativePath)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(i => i.ContentType)
                .HasMaxLength(100);

            builder.HasIndex(i => i.TabId);
            builder.HasIndex(i => i.UserId);

            builder.HasOne(i => i.User)
                .WithMany(u => u.Images)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

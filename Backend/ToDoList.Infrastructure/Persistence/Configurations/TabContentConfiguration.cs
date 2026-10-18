using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToDoList.Domain.Entities;

namespace ToDoList.Infrastructure.Persistence.Configurations
{
    public class TabContentConfiguration : IEntityTypeConfiguration<TabContent>
    {
        public void Configure(EntityTypeBuilder<TabContent> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.TextContent)
                .IsRequired();

            builder.HasIndex(c => c.TabId)
                .IsUnique();
        }
    }
}

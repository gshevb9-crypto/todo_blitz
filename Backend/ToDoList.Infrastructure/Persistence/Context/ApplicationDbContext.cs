using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ToDoList.Domain.Entities;

namespace ToDoList.Infrastructure.Persistence.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<RefreshTokenTable> RefreshTokens => Set<RefreshTokenTable>();
        public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
        public DbSet<Tab> Tabs => Set<Tab>();
        public DbSet<TabContent> TabContents => Set<TabContent>();
        public DbSet<TabImage> TabImages => Set<TabImage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }
    }
}

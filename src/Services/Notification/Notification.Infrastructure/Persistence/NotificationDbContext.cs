using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Notification.Infrastructure.Persistence;

public partial class NotificationDbContext : DbContext
{
    public NotificationDbContext() { }

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options) { }

    public virtual DbSet<Domain.Entities.Notification> Notifications { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    var builder = new ConfigurationBuilder()
    //        .SetBasePath(Directory.GetCurrentDirectory())
    //        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

    //    IConfigurationRoot configurationRoot = builder.Build();
    //    optionsBuilder.UseNpgsql(configurationRoot.GetConnectionString("stemifyresource"));
    //}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<Notification.Domain.Entities.Notification>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity
                .Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'");

            entity
                .Property(e => e.LastModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'");

            entity.Property(e => e.IsRead).HasDefaultValue(false);
        });
    }
}

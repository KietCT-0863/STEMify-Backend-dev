using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;
using System.Reflection;

namespace Order.Infrastructure.Persistence
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext() { }

        public OrderDbContext(DbContextOptions<OrderDbContext> options)
            : base(options) { }
        public DbSet<Domain.Entities.Order> Orders { get; set; } = null!;
        public DbSet<OrderHistory> OrderHistories { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Contract> Contracts { get; set; } = null!;
        public DbSet<LicenseAssignment> LicenseAssignments { get; set; } = null!;
        public DbSet<Organization> Organizations { get; set; } = null!;
        public DbSet<OrganizationSubscriptionOrder> OrganizationSubscriptionOrders { get; set; } = null!;
        public DbSet<OrganizationType> OrganizationTypes { get; set; } = null!;
        public DbSet<SubscriptionOrderCurriculum> SubscriptionOrderCurriculums { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}

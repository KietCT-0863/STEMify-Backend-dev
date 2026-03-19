using Microsoft.EntityFrameworkCore;
using Product.Domain.Entities;
using System.Reflection;

namespace Product.Infrastructure.Persistence
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext() { }

        public ProductDbContext(DbContextOptions<ProductDbContext> options)
            : base(options) { }

        public virtual DbSet<Plan> Plans { get; set; }
        public virtual DbSet<PlanBillingCycle> PlanBillingCycles { get; set; }
        public virtual DbSet<PlanCurriculum> PlanCurriculums { get; set; }
        public virtual DbSet<KitProduct> KitProducts { get; set; }
        public virtual DbSet<Component> Components { get; set; }
        public virtual DbSet<KitComponent> KitComponents { get; set; }
        public virtual DbSet<KitImage> KitImages { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

    }
}

using Cart.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Cart.Infrastructure.Persistence
{
    public class CartDbContext : DbContext
    {
        public CartDbContext() { }

        public CartDbContext(DbContextOptions<CartDbContext> options)
            : base(options) { }

        public DbSet<Domain.Entities.Cart> Carts { get; set; } = null!;

        public DbSet<CartItem> CartItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}

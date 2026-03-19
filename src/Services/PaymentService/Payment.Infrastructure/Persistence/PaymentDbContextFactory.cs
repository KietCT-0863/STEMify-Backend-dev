using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Payment.Infrastructure.Persistence
{
    public class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
    {
        public PaymentDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PaymentDbContext>();

            // Use a default connection string for migrations
            optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Database=stemifypayment;User Id=postgres;Password=postgres;");

            return new PaymentDbContext(optionsBuilder.Options);
        }
    }
}

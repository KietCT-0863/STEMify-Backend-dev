using Contracts.Abstractions.Services;
using Infrastructure.Common;
using Order.Domain.Constants;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence
{
    public class OrderDbContextSeed
    {
        private readonly OrderDbContext _dbContext;
        private readonly IFileReader _fileReader;

        public OrderDbContextSeed(OrderDbContext dbContext, IFileReader fileReader)
        {
            _dbContext = dbContext;
            _fileReader = fileReader;
        }

        public async Task SeedAsync()
        {
            var rootPath = AppCts.AbsoluteProjectPath;

            await new JsonDataSeeder<OrganizationType, OrderDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.OrganizationTypePath)
                .SeedAsync();

            await new JsonDataSeeder<Organization, OrderDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.OrganizationPath)
                .SeedAsync();
        }
    }
}

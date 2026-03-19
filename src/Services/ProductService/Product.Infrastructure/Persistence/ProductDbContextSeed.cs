using Contracts.Abstractions.Services;
using Infrastructure.Common;
using Product.Domain.Constants;
using Product.Domain.Entities;

namespace Product.Infrastructure.Persistence
{
    public class ProductDbContextSeed
    {
        private readonly ProductDbContext _dbContext;
        private readonly IFileReader _fileReader;

        public ProductDbContextSeed(ProductDbContext dbContext, IFileReader fileReader)
        {
            _dbContext = dbContext;
            _fileReader = fileReader;
        }

        public async Task SeedAsync()
        {
            var rootPath = AppCts.AbsoluteProjectPath;

            await new JsonDataSeeder<Plan, ProductDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.PlanPath)
                .SeedAsync();
            await new JsonDataSeeder<KitProduct, ProductDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.KitPath)
                .SeedAsync();
            await new JsonDataSeeder<KitImage, ProductDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.KitImagePath)
                .SeedAsync();
            await new JsonDataSeeder<Component, ProductDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.ComponentPath)
                .SeedAsync();
            await new JsonDataSeeder<KitComponent, ProductDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.KitComponentPath)
                .SeedAsync();
        }
    }
}

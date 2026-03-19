using Contracts.Abstractions.Persistence;
using Product.Domain.Entities;

namespace Product.Application.Common.Interfaces.Repositories
{
    public interface IKitImageRepository : IRepositoryBaseAsync<KitImage, int> { }
}

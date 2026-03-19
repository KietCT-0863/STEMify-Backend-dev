using Contracts.Abstractions.Persistence;
using Resource.Domain.Entities;

namespace Resource.Application.Common.Interfaces.Repositories
{
    public interface ILessonAssetRepository
        : IRepositoryBaseAsync<LessonAsset, int>
    { }

}

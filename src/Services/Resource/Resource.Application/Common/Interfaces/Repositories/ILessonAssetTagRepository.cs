using Contracts.Abstractions.Persistence;
using Resource.Domain.Entities;

namespace Resource.Application.Common.Interfaces.Repositories
{
    public interface ILessonAssetTagRepository
    : IRepositoryBaseAsync<LessonAssetTag, int>
    { }
}

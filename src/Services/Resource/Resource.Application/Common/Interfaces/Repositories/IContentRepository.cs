using Contracts.Abstractions.Persistence;
using Resource.Domain.Entities;

namespace Resource.Application.Common.Interfaces.Repositories;

public interface IContentRepository : IRepositoryBaseAsync<Content, int> { }

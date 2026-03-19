using Contracts.Abstractions.Persistence;
using Resource.Domain.Entities;

namespace Resource.Application.Common.Interfaces.Repositories;

public interface IAgeRangeRepository : IRepositoryBaseAsync<AgeRange, int> { }

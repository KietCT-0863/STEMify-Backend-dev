using Contracts.Abstractions.Persistence;

namespace Classroom.Application.Common.Interfaces.Repositories;

public interface IRubricScoreRepository : IRepositoryBaseAsync<Domain.Entities.RubricScore, int> { }

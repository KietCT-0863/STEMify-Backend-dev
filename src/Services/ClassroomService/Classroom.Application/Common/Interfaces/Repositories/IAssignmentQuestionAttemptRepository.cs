using Contracts.Abstractions.Persistence;

namespace Classroom.Application.Common.Interfaces.Repositories;

public interface IAssignmentQuestionAttemptRepository : IRepositoryBaseAsync<Domain.Entities.AssignmentQuestionAttempt, int> { }

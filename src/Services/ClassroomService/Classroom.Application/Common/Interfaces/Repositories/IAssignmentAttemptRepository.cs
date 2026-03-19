using Contracts.Abstractions.Persistence;

namespace Classroom.Application.Common.Interfaces.Repositories;

public interface IAssignmentAttemptRepository : IRepositoryBaseAsync<Domain.Entities.AssignmentAttempt, int> { }

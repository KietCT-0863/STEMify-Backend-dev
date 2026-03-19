using Contracts.Abstractions.Persistence;

namespace Classroom.Application.Common.Interfaces.Repositories;

public interface IStudentAssignmentRepository : IRepositoryBaseAsync<Domain.Entities.StudentAssignment, int> { }

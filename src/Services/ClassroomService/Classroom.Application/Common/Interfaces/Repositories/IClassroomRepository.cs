using Contracts.Abstractions.Persistence;

namespace Classroom.Application.Common.Interfaces.Repositories;

public interface IClassroomRepository : IRepositoryBaseAsync<Domain.Entities.Classroom, int> { }

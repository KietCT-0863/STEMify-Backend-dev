using Classroom.Domain.Entities;
using Contracts.Abstractions.Persistence;

namespace Classroom.Application.Common.Interfaces.Repositories;

public interface IAnnoucementRepository : IRepositoryBaseAsync<Annoucement, int> { }

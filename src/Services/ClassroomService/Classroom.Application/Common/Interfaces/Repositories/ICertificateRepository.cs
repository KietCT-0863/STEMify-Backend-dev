using Contracts.Abstractions.Persistence;

namespace Classroom.Application.Common.Interfaces.Repositories
{
    public interface ICertificateRepository : IRepositoryBaseAsync<Classroom.Domain.Entities.Certificate, int> { }
}

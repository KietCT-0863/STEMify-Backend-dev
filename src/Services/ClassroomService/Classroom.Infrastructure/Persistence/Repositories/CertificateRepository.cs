using Classroom.Application.Common.Interfaces.Repositories;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Classroom.Infrastructure.Persistence.Repositories
{
    public class CertificateRepository
        : EfRepositoryBase<ClassroomDbContext, Domain.Entities.Certificate, int>,
            ICertificateRepository
    {
        public CertificateRepository(ClassroomDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}

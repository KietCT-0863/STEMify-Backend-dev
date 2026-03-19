using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using MediatR;


namespace Identity.Application.Users.Commands.CreateJobRole
{
    public class CreateJobRoleCommandHandler : IRequestHandler<CreateJobRoleCommand>
    {
        private readonly IIdentityUnitOfWork _unitOfWork;
        public CreateJobRoleCommandHandler(IIdentityUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(CreateJobRoleCommand request, CancellationToken cancellationToken)
        {
            var jobRoles = new List<JobRole>();
            foreach (var name in request.Names)
            {
                var jobRole = new JobRole
                {
                    Name = name
                };
                jobRoles.Add(jobRole);
            }
            await _unitOfWork.JobRoles.AddRangeAsync(jobRoles, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

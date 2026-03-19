using Identity.Application.Common.Interfaces;
using MediatR;
using Shared.Protos.User;

namespace Identity.Application.Users.Queries.GetJobRoles
{
    public class GetJobRolesQueryHandler : IRequestHandler<GetJobRolesQuery, PagedJobRoleList>
    {
        private readonly IIdentityUnitOfWork _unitOfWork;

        public GetJobRolesQueryHandler(IIdentityUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<PagedJobRoleList> Handle(GetJobRolesQuery request, CancellationToken cancellationToken)
        {
            var jobRoles = await _unitOfWork.JobRoles.GetAllAsync();
            var pagedResponse = new PagedJobRoleList
            {
                TotalCount = jobRoles.Count,
                PageNumber = 1,
                PageSize = jobRoles.Count,
            };
            foreach (var jobRole in jobRoles)
            {
                var jobRoleResponse = new JobRoleResponse
                {
                    Id = jobRole.Id,
                    Name = jobRole.Name,
                };

                pagedResponse.Items.Add(jobRoleResponse);
            }
            return pagedResponse;
        }
    }
}

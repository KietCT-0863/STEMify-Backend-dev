using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Protos.User;
using System.Linq.Expressions;

namespace Identity.Application.Users.Queries.GetContacts
{
    public class GetContactsQueryHandler : IRequestHandler<GetContactsQuery, PagedContactList>
    {
        private readonly IIdentityUnitOfWork _unitOfWork;

        public GetContactsQueryHandler(IIdentityUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<PagedContactList> Handle(GetContactsQuery request, CancellationToken cancellationToken)
        {
            Expression<Func<Contact, bool>> predicate = c =>
                (string.IsNullOrEmpty(request.Search) || c.Email.ToLower().Contains(request.Search) || c.PhoneNumber.Contains(request.Search)) &&
                (!request.Status.HasValue || c.Status == request.Status.Value);

            Expression<Func<Contact, object>>? sortExpression = c => c.CreatedDate;

            // Define projection function
            Func<IQueryable<Contact>, IQueryable<ContactResponse>> projectionFunc = query =>
                query
                .Include(c => c.JobRole)
                .Select(c => new ContactResponse
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber,
                    OrganizationName = c.OrganizationName,
                    JobRole = c.JobRole.Name,
                    CreatedAt = c.CreatedDate.ToString(),
                    UpdatedAt = c.LastModifiedDate.HasValue ? c.LastModifiedDate.Value.ToString() : "",
                    JobRoleId = c.JobRoleId,
                    Status = c.Status.ToString()
                });

            var paged = await _unitOfWork.Contacts.GetByPageFilter(
                pageRequest: new PageRequest
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                },
                projectionFunc: projectionFunc,
                sortExpression: sortExpression,
                predicate: predicate,
                descending: true,
                cancellationToken: cancellationToken
            );

            var response = new PagedContactList
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };
            response.Items.AddRange(paged.Items);

            return response;
        }
    }
}

using Google.Protobuf.WellKnownTypes;
using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.Common.Interfaces;
using Order.Application.Models;
using Order.Domain.Entities;
using Shared.Protos.Order;
using System.Linq.Expressions;

namespace Order.Application.Queries.Contracts.GetContractList
{
    public class GetContractListQueryHandler
        : IRequestHandler<GetContractListQuery, GrpcPagedContractResponse>
    {
        private readonly IOrderUnitOfWork _unitOfWork;

        public GetContractListQueryHandler(IOrderUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GrpcPagedContractResponse> Handle(
            GetContractListQuery request,
            CancellationToken cancellationToken
        )
        {
            var search = request.Search?.ToLower();

            Expression<Func<Contract, bool>> predicate = c =>
                (string.IsNullOrEmpty(search) || c.Name.ToLower().Contains(search)) &&
                (!request.Status.HasValue || c.Status == request.Status.Value) &&
                (!request.OrganizationId.HasValue || c.OrganizationId == request.OrganizationId.Value);

            Expression<Func<Contract, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "name" => c => c.Name,
                    "createddate" => c => c.CreatedDate,
                    _ => c => c.Name,
                };

            Func<IQueryable<Contract>, IQueryable<ContractDto>> projectionFunc = query =>
               query
                   .Include(organization => organization.Organization)
                        .ThenInclude(org => org.OrganizationType)
                   .Select(organization => new ContractDto
                   {
                       Id = organization.Id,
                       Name = organization.Name,
                       Description = organization.Description ?? string.Empty,
                       Status = organization.Status.ToString(),
                       FileUrl = organization.FileUrl,
                       OrganizationId = organization.Organization.Id,
                       OrganizationType = organization.Organization.OrganizationType.Name,
                       OrganizationName = organization.Organization.Name,
                       OrganizationImageUrl = organization.Organization.ImageUrl,
                       CreatedDate = organization.CreatedDate,
                       LastModifiedDate = organization.LastModifiedDate,
                   });

            var paged = await _unitOfWork.Contracts.GetByPageFilter(
                pageRequest: new PageRequest
                {
                    PageNumber = request.PageNumber ?? 1,
                    PageSize = request.PageSize ?? 10,
                },
                projectionFunc: projectionFunc,
                sortExpression: sortExpression,
                predicate: predicate,
                descending: request.IsDescending,
                cancellationToken: cancellationToken
            );

            var dtoItems = paged.Items.ToList();
            var organizationDetails = dtoItems.Select(dto =>
            {
                var detail = new GrpcContractDetail
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Description = dto.Description,
                    Status = dto.Status,
                    FileUrl = dto.FileUrl,
                    Organization = new GrpcOrganizationInformation
                    {
                        Id = dto.OrganizationId,
                        Name = dto.OrganizationName,
                        ImageUrl = dto.OrganizationImageUrl,
                        OrganizationType = dto.OrganizationType,
                    },
                    CreatedDate = Timestamp.FromDateTimeOffset(dto.CreatedDate),
                    LastModifiedDate = dto.LastModifiedDate.HasValue
                        ? Timestamp.FromDateTimeOffset(dto.LastModifiedDate.Value)
                        : null
                };
                return detail;
            }).ToList();

            var response = new GrpcPagedContractResponse
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            response.Items.AddRange(organizationDetails);

            return response;
        }
    }
}
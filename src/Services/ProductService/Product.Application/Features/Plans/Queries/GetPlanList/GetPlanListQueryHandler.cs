using Google.Protobuf.WellKnownTypes;
using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product.Application.Common.Interfaces;
using Product.Application.Common.Interfaces.Cache;
using Product.Application.Models;
using Product.Domain.Entities;
using Shared.Protos.Product;
using System.Linq.Expressions;

namespace Product.Application.Features.Plans.Queries.GetPlanList
{
    public class GetPlanListQueryHandler
        : IRequestHandler<GetPlanListQuery, GrpcPagedPlanResponse>
    {
        private readonly IProductUnitOfWork _unitOfWork;
        private readonly ICurriculumCacheService _curriculumCacheService;

        public GetPlanListQueryHandler(IProductUnitOfWork unitOfWork, ICurriculumCacheService curriculumCacheService)
        {
            _unitOfWork = unitOfWork;
            _curriculumCacheService = curriculumCacheService;
        }

        public async Task<GrpcPagedPlanResponse> Handle(
            GetPlanListQuery request,
            CancellationToken cancellationToken
        )
        {
            var search = request.Search?.ToLower();
            var billingCycle = request.BillingCycle;

            Expression<Func<Plan, bool>> predicate = c =>
                (string.IsNullOrEmpty(search) ||
                 c.Name.ToLower().Contains(search) ||
                 (c.Description != null && c.Description.ToLower().Contains(search)) ||
                 (c.AccessSupportDetail != null && c.AccessSupportDetail.ToLower().Contains(search))) &&
                 (request.Status.HasValue ? request.Status.Value == c.Status : c.Status != Domain.Enums.PlanStatus.Archived) &&
                (!billingCycle.HasValue || c.PlanBillingCycles.Any(pbc => pbc.BillingCycle == billingCycle)) &&
                (!request.IsAddOn.HasValue || c.PlanBillingCycles.Any(pbc => pbc.IsAddOn == request.IsAddOn));

            // keep sortExpression as before (may box different types to object)
            Expression<Func<Plan, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "name" => c => c.Name,
                    "createdate" => c => c.CreatedDate,
                    _ => c => c.CreatedDate,
                };

            Func<IQueryable<Plan>, IQueryable<PlanDto>> projectionFunc = query =>
                query
                    .Include(plan => plan.PlanCurriculums)
                    .Include(plan => plan.PlanBillingCycles)
                    .Select(plan => new PlanDto
                    {
                        Id = plan.Id,
                        Name = plan.Name,
                        Status = plan.Status.ToString(),
                        Description = plan.Description ?? string.Empty,
                        AccessSupportDetail = plan.AccessSupportDetail ?? string.Empty,
                        CurriculumCount = plan.CurriculumCount,
                        MaxTeacherSeats = plan.MaxTeacherSeats,
                        MaxStudentSeats = plan.MaxStudentSeats,
                        CreatedDate = plan.CreatedDate,
                        LastModifiedDate = plan.LastModifiedDate,
                        Curriculums = plan.PlanCurriculums
                            .Select(pc => new PlanCurriculumDto { Id = pc.CurriculumId })
                            .ToList(),
                        PlanBillingCycles = plan.PlanBillingCycles
                            .Select(pbc => new PlanBillingCycleDto
                            {
                                Id = pbc.Id,
                                PlanId = pbc.PlanId,
                                BillingCycle = pbc.BillingCycle.ToString(),
                                Price = pbc.Price,
                                MaxTeacherSeats = pbc.MaxTeacherSeats,
                                MaxStudentSeats = pbc.MaxStudentSeats,
                                IsAddOn = pbc.IsAddOn,
                                ParentPlanBillingCycleId = pbc.ParentPlanBillingCycleId
                            })
                            .ToList()
                    });

            var paged = await _unitOfWork.Plans.GetByPageFilter(
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
            var planDetails = dtoItems.Select(dto =>
            {
                var detail = new GrpcPlanDetail
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Status = dto.Status,
                    Description = dto.Description,
                    AccessSupportDetail = dto.AccessSupportDetail,
                    CurriculumCount = dto.CurriculumCount,
                    MaxTeacherSeats = dto.MaxTeacherSeats,
                    MaxStudentSeats = dto.MaxStudentSeats,
                    CreatedAt = Timestamp.FromDateTimeOffset(dto.CreatedDate),
                    UpdatedAt = dto.LastModifiedDate.HasValue
                        ? Timestamp.FromDateTimeOffset(dto.LastModifiedDate.Value)
                        : null
                };

                if (dto.Curriculums != null)
                {
                    foreach (var pc in dto.Curriculums)
                    {
                        detail.Curriculums.Add(new GrpcPlanCurriculumModel
                        {
                            Id = pc.Id
                        });
                    }
                }

                if (dto.PlanBillingCycles != null)
                {
                    foreach (var pb in dto.PlanBillingCycles)
                    {
                        detail.PlanBillingCycles.Add(new GrpcPlanBillingCycleDetail
                        {
                            Id = pb.Id,
                            PlanId = pb.PlanId,
                            BillingCycle = pb.BillingCycle,
                            Price = (double)pb.Price,
                            MaxTeacherSeats = pb.MaxTeacherSeats,
                            MaxStudentSeats = pb.MaxStudentSeats,
                            IsAddOn = pb.IsAddOn,
                            ParentPlanBillingCycleId = pb.ParentPlanBillingCycleId
                        });
                    }
                }

                return detail;
            }).ToList();

            var enrichmentTasks = new List<Task>();
            foreach (var item in planDetails)
            {
                foreach (var curr in item.Curriculums)
                {
                    enrichmentTasks.Add(EnrichCurriculumAsync(curr, cancellationToken));
                }
            }

            if (enrichmentTasks.Count > 0)
                await Task.WhenAll(enrichmentTasks);

            var response = new GrpcPagedPlanResponse
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            response.Items.AddRange(planDetails);

            return response;
        }

        private async Task EnrichCurriculumAsync(GrpcPlanCurriculumModel curr, CancellationToken cancellationToken)
        {
            try
            {
                var curriculum = await _curriculumCacheService.GetCurriculumByIdAsync(curr.Id, cancellationToken);
                if (curriculum != null)
                {
                    curr.Title = curriculum.Title ?? "Unknown";
                    curr.ImageUrl = curriculum.ImageUrl ?? string.Empty;
                }
                else
                {
                    curr.Title = "Unknown";
                    curr.ImageUrl = string.Empty;
                }
            }
            catch
            {
                curr.Title = "Unknown";
                curr.ImageUrl = string.Empty;
            }
        }
    }
}
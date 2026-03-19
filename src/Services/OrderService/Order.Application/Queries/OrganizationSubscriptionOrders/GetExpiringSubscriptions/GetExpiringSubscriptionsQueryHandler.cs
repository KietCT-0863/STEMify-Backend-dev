using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.Common.Interfaces;
using Order.Application.Models;
using Order.Domain.Enums;
using System.Linq.Expressions;

namespace Order.Application.Queries.OrganizationSubscriptionOrders.GetExpiringSubscriptions
{
    public class GetExpiringSubscriptionsQueryHandler
        : IRequestHandler<GetExpiringSubscriptionsQuery, List<ExpiringSubscriptionDto>>
    {
        private readonly IOrderUnitOfWork _unitOfWork;

        public GetExpiringSubscriptionsQueryHandler(IOrderUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ExpiringSubscriptionDto>> Handle(
            GetExpiringSubscriptionsQuery request,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var expiryThreshold = now.AddDays(request.WarningDays);

            // Query subscriptions that are:
            // 1. Active
            // 2. Expiring within the warning period (EndDate between now and now + warningDays)
            // 3. Optional: Filter by OrganizationId
            Expression<Func<Domain.Entities.OrganizationSubscriptionOrder, bool>> predicate = s =>
                s.Status == OrganizationSubscriptionOrderStatus.Active
                && s.EndDate >= now
                && s.EndDate <= expiryThreshold
                && (!request.OrganizationId.HasValue || s.OrganizationId == request.OrganizationId.Value);

            // Use ProjectBy to get the data with related entities
            Func<IQueryable<Domain.Entities.OrganizationSubscriptionOrder>, IQueryable<ExpiringSubscriptionDto>> projectionFunc = query =>
                query
                    .Include(s => s.Organization)
                    .Include(s => s.LicenseAssignments)
                    .Select(s => new ExpiringSubscriptionDto
                    {
                        SubscriptionOrderId = s.Id,
                        OrganizationId = s.OrganizationId,
                        OrganizationName = s.Organization != null ? s.Organization.Name : $"Organization {s.OrganizationId}",
                        PlanName = s.PlanName,
                        Code = s.Code,
                        ExpiryDate = s.EndDate,
                        DaysUntilExpiry = (int)(s.EndDate - now).TotalDays,
                        MaxStudentSeats = s.MaxStudentSeats,
                        MaxTeacherSeats = s.MaxTeacherSeats,
                        CurrentStudentSeats = s.LicenseAssignments != null ? s.LicenseAssignments.Count(la =>
                            la.LicenseType == LicenseType.Student &&
                            (la.Status == LicenseAssignmentStatus.Active ||
                             la.Status == LicenseAssignmentStatus.Pending)) : 0,
                        CurrentTeacherSeats = s.LicenseAssignments != null ? s.LicenseAssignments.Count(la =>
                            la.LicenseType == LicenseType.Teacher &&
                            (la.Status == LicenseAssignmentStatus.Active ||
                             la.Status == LicenseAssignmentStatus.Pending)) : 0,
                        AdminUserIds = new List<string>(),
                        AdminEmails = new List<string>()
                    });

            var result = _unitOfWork.OrganizationSubscriptionOrders
                .ProjectBy<ExpiringSubscriptionDto, DateTime>(
                    projectionFunc,
                    sortExpression: s => s.EndDate,
                    predicate: predicate
                )
                .ToList();

            return await Task.FromResult(result);
        }
    }
}

using System.Linq.Expressions;
using Infrastructure.Common.Paging;
using MediatR;
using Notification.Application.Common.Interfaces;
using Notification.Application.Models;
using Notification.Application.Queries;
using Shared.Protos.Notification;

namespace Notification.Application.Handlers
{
    public class QueryNotificationsQueryHandler
        : IRequestHandler<QueryNotificationsQuery, PagedNotificationList>
    {
        private readonly INotificationUnitOfWork _unitOfWork;

        public QueryNotificationsQueryHandler(INotificationUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedNotificationList> Handle(
            QueryNotificationsQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var filter = new NotificationParams
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                    PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                    OrderBy = request.OrderBy,
                    UserId = request.UserId,
                    IsRead = request.IsRead,
                };

                var pageRequest = filter.ToPageRequest();

                Expression<Func<Domain.Entities.Notification, bool>> predicate = c =>
                    (
                        string.IsNullOrEmpty(filter.Search)
                        || c.Title.ToLower().Contains(filter.Search)
                        || c.Message.ToLower().Contains(filter.Search)
                    )
                    && (string.IsNullOrEmpty(filter.UserId) || c.UserId == filter.UserId)
                    && (!filter.IsRead.HasValue || c.IsRead == filter.IsRead.Value);

                Expression<Func<Domain.Entities.Notification, object>>? sortExpression =
                    request.OrderBy?.ToLower() switch
                    {
                        "title" => c => c.Title,
                        "createddate" => c => c.CreatedDate,
                        "lastmodifieddate" => c => c.LastModifiedDate ?? DateTime.MinValue,
                        _ => c => c.CreatedDate,
                    };

                var paged = await _unitOfWork.Notifications.GetByPageFilter(
                    pageRequest,
                    sortExpression: sortExpression,
                    predicate: predicate,
                    cancellationToken: cancellationToken
                );

                var response = new PagedNotificationList
                {
                    TotalCount = paged.TotalCount,
                    PageNumber = paged.PageNumber,
                    PageSize = paged.PageSize,
                    TotalPages = paged.TotalPages,
                };

                foreach (var notification in paged.Items)
                {
                    var notificationResponse = new NotificationResponse
                    {
                        Id = notification.Id,
                        Title = notification.Title,
                        Message = notification.Message,
                        UserId = notification.UserId,
                        IsRead = notification.IsRead,
                        ClickUrl = notification.ClickUrl,
                        CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                            notification.CreatedDate
                        ),
                        LastModifiedDate = notification.LastModifiedDate.HasValue
                            ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                                notification.LastModifiedDate.Value
                            )
                            : null,
                    };

                    response.Items.Add(notificationResponse);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the notification list: {ex.Message}",
                    ex
                );
            }
        }
    }
}

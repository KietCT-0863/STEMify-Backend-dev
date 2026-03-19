using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Notification.Application.Commands;
using Notification.Application.Queries;
using Shared.Protos.Notification;

namespace Notification.API.Services
{
    public class NotificationGrpcService : NotificationService.NotificationServiceBase
    {
        private readonly IMediator _mediator;

        public NotificationGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<NotificationResponse> CreateNotification(
            CreateNotificationRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var command = new CreateNotificationCommand
                {
                    Title = request.Title,
                    ClickUrl = request.ClickUrl,
                    Message = request.Message,
                    UserId = request.UserId,
                };

                var result = await _mediator.Send(command);
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"CreateNotification failed: {ex.Message}")
                );
            }
        }

        public override async Task<NotificationResponse> GetNotification(
            GetNotificationRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var query = new GetNotificationByIdQuery(request.Id);
                var result = await _mediator.Send(query);

                if (result == null)
                    throw new RpcException(
                        new Status(
                            StatusCode.NotFound,
                            $"Notification with ID {request.Id} not found."
                        )
                    );

                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"GetNotification failed: {ex.Message}")
                );
            }
        }

        public override async Task<NotificationResponse> UpdateNotification(
            UpdateNotificationRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var command = new UpdateNotificationCommand
                {
                    Id = request.Id,
                    IsRead = request.IsRead,
                };

                var result = await _mediator.Send(command);
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"UpdateNotification failed: {ex.Message}")
                );
            }
        }

        public override async Task<Empty> DeleteNotification(
            DeleteNotificationRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var command = new DeleteNotificationCommand { Id = request.Id };
                await _mediator.Send(command);

                return new Empty();
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"DeleteNotification failed: {ex.Message}")
                );
            }
        }

        public override async Task<NotificationList> ListNotifications(
            Empty request,
            ServerCallContext context
        )
        {
            try
            {
                var result = await _mediator.Send(new GetNotificationListQuery());
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"ListNotifications failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedNotificationList> QueryNotifications(
            QueryNotificationsRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var query = new QueryNotificationsQuery
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    OrderBy = request.OrderBy,
                    UserId = request.UserId,
                    IsRead = request.IsRead,
                };
                var result = await _mediator.Send(query);

                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"QueryNotifications failed: {ex.Message}")
                );
            }
        }
    }
}

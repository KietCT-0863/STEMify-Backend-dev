using FluentValidation;
using MediatR;
using Shared.Protos.Notification;

namespace Notification.Application.Queries
{
    public class GetNotificationByIdQuery : IRequest<NotificationResponse>
    {
        public int Id { get; set; }

        public GetNotificationByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetNotificationByIdQueryValidator : AbstractValidator<GetNotificationByIdQuery>
    {
        public GetNotificationByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}

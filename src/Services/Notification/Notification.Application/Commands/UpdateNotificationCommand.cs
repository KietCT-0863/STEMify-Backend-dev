using FluentValidation;
using MediatR;
using Shared.Protos.Notification;

namespace Notification.Application.Commands
{
    public class UpdateNotificationCommand : IRequest<NotificationResponse>
    {
        public int Id { get; set; }
        public bool IsRead { get; set; }
    }

    public class UpdateNotificationCommandValidator : AbstractValidator<UpdateNotificationCommand>
    {
        public UpdateNotificationCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Notification ID must be greater than 0.");
        }
    }
}

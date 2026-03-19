using FluentValidation;
using MediatR;

namespace Notification.Application.Commands
{
    public class DeleteNotificationCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteNotificationCommandValidator : AbstractValidator<DeleteNotificationCommand>
    {
        public DeleteNotificationCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Notification ID must be greater than 0.");
        }
    }
}

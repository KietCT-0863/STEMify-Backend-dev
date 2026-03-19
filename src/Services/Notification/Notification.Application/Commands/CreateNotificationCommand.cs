using FluentValidation;
using MediatR;
using Shared.Protos.Notification;

namespace Notification.Application.Commands
{
    public class CreateNotificationCommand : IRequest<NotificationResponse>
    {
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string ClickUrl { get; set; }
    }

    public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
    {
        public CreateNotificationCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(100)
                .WithMessage("Title must not exceed 100 characters.");
            RuleFor(x => x.Message)
                .NotEmpty()
                .WithMessage("Message is required.")
                .MaximumLength(500)
                .WithMessage("Message must not exceed 500 characters.");
            RuleFor(x => x.ClickUrl)
                .NotEmpty()
                .WithMessage("Click URL is required.")
                .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("Click URL must be a valid absolute URL.");
        }
    }
}

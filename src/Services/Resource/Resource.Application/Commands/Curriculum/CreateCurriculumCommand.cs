using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Curriculum
{
    public class CreateCurriculumCommand : IRequest<CurriculumResponse>
    {
        public string Title { get; set; }
        public string Code { get; set; }
        public byte[] ImageBytes { get; set; }
        public string Description { get; set; }
        public string CreatedByUserId { get; set; }
    }

    public class CreateCurriculumCommandValidator : AbstractValidator<CreateCurriculumCommand>
    {
        public CreateCurriculumCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(255)
                .WithMessage("Title must not exceed 255 characters.");

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Code is required.")
                .MaximumLength(255)
                .WithMessage("Code must not exceed 50 characters.");

            RuleFor(x => x.ImageBytes)
                .Must(bytes => bytes == null || bytes.Length <= 5 * 1024 * 1024)
                .WithMessage("Image size must not exceed 5 MB.");

            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");

            RuleFor(x => x.CreatedByUserId).NotEmpty().WithMessage("CreatedByUserId is required.");
        }
    }
}

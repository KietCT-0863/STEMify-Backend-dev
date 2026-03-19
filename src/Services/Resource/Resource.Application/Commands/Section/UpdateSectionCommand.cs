using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Section
{
    public class UpdateSectionCommand : IRequest<SectionResponse>
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public string? Title { get; set; }
        public int? Duration { get; set; }
        public Domain.Enums.SectionStatus? Status { get; set; }
        public bool? IsVisibleToStudent { get; set; }
    }

    public class UpdateSectionCommandValidator : AbstractValidator<UpdateSectionCommand>
    {
        public UpdateSectionCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Section ID must be greater than 0.");

            RuleFor(x => x.Status).IsInEnum().WithMessage("Status must be a valid enum value.");
        }
    }
}

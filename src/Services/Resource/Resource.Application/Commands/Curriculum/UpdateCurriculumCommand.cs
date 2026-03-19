using FluentValidation;
using MediatR;
using Resource.Domain.Enums;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Curriculum
{
    public class UpdateCurriculumCommand : IRequest<CurriculumResponse>
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Code { get; set; }
        public byte[]? ImageBytes { get; set; }
        public string? Description { get; set; }
        public CurriculumStatus? Status { get; set; }
    }

    public class UpdateCurriculumCommandValidator : AbstractValidator<UpdateCurriculumCommand>
    {
        public UpdateCurriculumCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Curriculum ID must be greater than 0.");
        }
    }
}

using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.Curriculum
{
    public class DeleteCurriculumCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteCurriculumCommandValidator : AbstractValidator<DeleteCurriculumCommand>
    {
        public DeleteCurriculumCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Curriculum ID must be greater than 0.");
        }
    }
}

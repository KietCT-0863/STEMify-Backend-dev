using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.CourseLearningOutcome
{
    public class UpdateCourseLearningOutcomeCommand : IRequest<CourseLearningOutcomeResponse>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? CourseId { get; set; }
        public List<int> ProgramLearningOutcomeIds { get; set; } = new List<int>();
    }

    public class UpdateCourseLearningOutcomeCommandValidator : AbstractValidator<UpdateCourseLearningOutcomeCommand>
    {
        public UpdateCourseLearningOutcomeCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("CourseLearningOutcome ID must be greater than 0.");
        }
    }
}

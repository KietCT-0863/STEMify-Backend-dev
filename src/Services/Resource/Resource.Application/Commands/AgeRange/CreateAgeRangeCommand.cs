using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.AgeRange
{
    public class CreateAgeRangeCommand : IRequest<AgeRangeResponse>
    {
        public string AgeRangeLabel { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
    }

    public class CreateAgeRangeCommandValidator : AbstractValidator<CreateAgeRangeCommand>
    {
        public CreateAgeRangeCommandValidator()
        {
            RuleFor(x => x.AgeRangeLabel)
                .NotEmpty()
                .WithMessage("Age range label is required.")
                .MaximumLength(100)
                .WithMessage("Age range label must not exceed 100 characters.");

            RuleFor(x => x.MinAge)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Min age must be at least 0.");

            RuleFor(x => x.MaxAge)
                .GreaterThanOrEqualTo(x => x.MinAge)
                .WithMessage("Max age must be greater than or equal to Min age.");

            RuleFor(x => x)
                .Must(x => x.MinAge < x.MaxAge)
                .WithMessage("Min age must be less than Max age.");
        }
    }
}

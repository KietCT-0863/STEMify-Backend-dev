using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Standard
{
    public class UpdateStandardCommand : IRequest<StandardResponse>
    {
        public int Id { get; set; }
        public string? StandardName { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateStandardCommandValidator : AbstractValidator<UpdateStandardCommand>
    {
        public UpdateStandardCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Standard ID must be greater than 0.");
        }
    }
}

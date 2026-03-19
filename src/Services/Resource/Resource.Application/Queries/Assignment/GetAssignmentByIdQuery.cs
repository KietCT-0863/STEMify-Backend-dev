using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Assignment
{
    public class GetAssignmentByIdQuery : IRequest<GrpcAssignmentModel>
    {
        public int Id { get; set; }

        public GetAssignmentByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetAssignmentByIdQueryValidator : AbstractValidator<GetAssignmentByIdQuery>
    {
        public GetAssignmentByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}

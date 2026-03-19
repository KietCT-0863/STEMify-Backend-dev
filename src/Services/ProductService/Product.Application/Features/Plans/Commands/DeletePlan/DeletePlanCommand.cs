using MediatR;

namespace Product.Application.Features.Plans.Commands.DeletePlan
{
    public class DeletePlanCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }
}

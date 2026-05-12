using MediatR;
using Resource.Application.Commands.Assignment;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.Assignment
{
    public class DeleteAssignmentsCommandHandler
        : IRequestHandler<DeleteAssignmentsCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteAssignmentsCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteAssignmentsCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _unitOfWork.Assignments.FindByIdForUpdateAsync(request.Id, cancellationToken);
            if (assignment == null)
                throw new KeyNotFoundException($"Assignment with ID {request.Id} not found.");

            var questions = await _unitOfWork.AssignmentQuestions.FindAsync(q => q.AssignmentId == request.Id, cancellationToken);
            if (questions != null)
            {
                foreach (var q in questions.ToList())
                {
                    await _unitOfWork.AssignmentQuestions.DeleteAsync(q, cancellationToken);
                }
            }

            await _unitOfWork.Assignments.DeleteAsync(assignment, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
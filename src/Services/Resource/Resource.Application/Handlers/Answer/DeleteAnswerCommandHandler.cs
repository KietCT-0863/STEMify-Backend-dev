using MediatR;
using Resource.Application.Commands.Answer;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.Answer
{
    public class DeleteAnswerCommandHandler : IRequestHandler<DeleteAnswerCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteAnswerCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteAnswerCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var answer = await _unitOfWork.Answers.FindByIdAsync(request.Id, cancellationToken);
                if (answer == null)
                    throw new KeyNotFoundException($"Answer with ID {request.Id} not found.");

                await _unitOfWork.Answers.DeleteAsync(answer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while deleting the answer: {ex.Message}",
                    ex
                );
            }
        }
    }
}

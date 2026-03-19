using MediatR;
using Resource.Application.Commands.Quiz;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.Quiz
{
    public class DeleteQuizCommandHandler : IRequestHandler<DeleteQuizCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteQuizCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteQuizCommand request, CancellationToken cancellationToken)
        {

            var quiz = await _unitOfWork.Quizzes.FindByIdAsync(request.Id, cancellationToken);
            if (quiz == null)
                throw new KeyNotFoundException($"Quiz with ID {request.Id} not found.");

            await _unitOfWork.Quizzes.DeleteAsync(quiz, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        }
    }
}

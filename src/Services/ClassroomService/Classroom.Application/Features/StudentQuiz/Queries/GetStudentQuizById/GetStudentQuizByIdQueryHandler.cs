using Classroom.Application.Common.Interfaces;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Specifications.StudentQuiz;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentQuiz.Queries.GetStudentQuizById
{
    public class GetStudentQuizByIdQueryHandler : IRequestHandler<GetStudentQuizByIdQuery, GrpcStudentQuizResponse>
    {
        private readonly ILogger<GetStudentQuizByIdQueryHandler> _logger;
        private readonly IClassroomUnitOfWork _unitOfWork;
        public GetStudentQuizByIdQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            ILogger<GetStudentQuizByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<GrpcStudentQuizResponse> Handle(GetStudentQuizByIdQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetStudentQuizByIdSpecification(request.Id);
            var studentQuiz = await _unitOfWork.StudentQuizzes
                .FirstOrDefaultAsync(spec, cancellationToken);
            if (studentQuiz == null)
            {
                _logger.LogWarning("StudentQuiz with Id: {Id} not found", request.Id);
                throw new KeyNotFoundException($"StudentQuiz with Id {request.Id} not found.");
            }
            return studentQuiz.ToGprcStudentQuizResponse();
        }
    }
}

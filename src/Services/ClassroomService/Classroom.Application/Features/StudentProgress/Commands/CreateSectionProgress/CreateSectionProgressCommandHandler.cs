using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Features.StudentAssignment.Commands.CreateStudentAssignment;
using Classroom.Application.Features.StudentQuiz.Commands.CreateStudentQuiz;
using Classroom.Application.Specifications.StudentProgress;
using Classroom.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Classroom.Application.Features.StudentProgress.Commands.CreateSectionProgress
{
    public class CreateSectionProgressCommandHandler : IRequestHandler<CreateSectionProgressCommand, Unit>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ILogger<CreateSectionProgressCommandHandler> _logger;
        private readonly IGrpcQuizClient _grpcQuizClient;
        private readonly IGrpcAssignmentClient _grpcAssignmentClient;
        private readonly IGrpcContentClient _grpcContentClient;
        private readonly IMediator _mediator;

        public CreateSectionProgressCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            ILogger<CreateSectionProgressCommandHandler> logger,
            IGrpcContentClient grpcContentClient,
            IMediator mediator,
            IGrpcAssignmentClient grpcAssignmentClient,
            IGrpcQuizClient grpcQuizClient)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _grpcContentClient = grpcContentClient ?? throw new ArgumentNullException(nameof(grpcContentClient));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _grpcAssignmentClient = grpcAssignmentClient ?? throw new ArgumentNullException(nameof(grpcAssignmentClient));
            _grpcQuizClient = grpcQuizClient ?? throw new ArgumentNullException(nameof(grpcQuizClient));
        }

        public async Task<Unit> Handle(CreateSectionProgressCommand request, CancellationToken cancellationToken)
        {
            var sectionProgress = new StudentSectionProgress
            {
                StudentLessonProgressId = request.StudentLessonProgressId,
                SectionId = request.SectionId,
                Status = request.Status,
            };
            await _unitOfWork.SectionProgress.AddAsync(sectionProgress, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created section progress for SectionId: {SectionId}, StudentLessonProgressId: {StudentLessonProgressId}", request.SectionId, request.StudentLessonProgressId);

            var content = await _grpcContentClient.GetContentBySectionIdAsync(request.SectionId);

            // if the content is a quiz, create a student quiz
            if (content != null && content.ContentType == "Quiz" && content.QuizId.HasValue)
            {
                var spec = new GetStudentSectionProgressByIdSpec(sectionProgress.Id);
                sectionProgress = await _unitOfWork.SectionProgress.FirstOrDefaultAsync(spec, cancellationToken);

                var quizDetails = await _grpcQuizClient.GetQuizByIdAsync(content.QuizId.Value);

                if (sectionProgress != null)
                {
                    var createStudentQuizCommand = new CreateStudentQuizCommand
                    {
                        StudentId = sectionProgress.LessonProgress.CourseEnrollment.StudentId.ToString(),
                        QuizId = content.QuizId.Value,
                        TimeLimitMinutes = content.TimeLimitInMinutes,
                        DueDate = content.DurationDays.HasValue ? DateTime.UtcNow.AddDays(content.DurationDays.Value) : null,
                        StudentSectionProgressId = sectionProgress.Id,
                        MaxAttemptAllowed = quizDetails?.MaxAttemptAllowed
                    };
                    await _mediator.Send(createStudentQuizCommand, cancellationToken);
                }
            }
            else if (content != null && content.ContentType == "Assignment" && content.AssignmentId.HasValue)
            {
                var spec = new GetStudentSectionProgressByIdSpec(sectionProgress.Id);
                sectionProgress = await _unitOfWork.SectionProgress.FirstOrDefaultAsync(spec, cancellationToken);

                var assignmentDetails = await _grpcAssignmentClient.GetAssignmentByIdAsync(content.AssignmentId.Value);

                if (sectionProgress != null)
                {
                    var createStudentAssignmentCommand = new CreateStudentAssignmentCommand
                    {
                        StudentId = sectionProgress.LessonProgress.CourseEnrollment.StudentId.ToString(),
                        AssignmentId = content.AssignmentId.Value,
                        DueDate = content.DurationDays.HasValue ? DateTime.UtcNow.AddDays(content.DurationDays.Value) : null,
                        StudentSectionProgressId = sectionProgress.Id,
                        MaxAttemptAllowed = assignmentDetails?.MaxAttemptAllowed
                    };
                    await _mediator.Send(createStudentAssignmentCommand, cancellationToken);
                }
            }

            return Unit.Value;
        }
    }
}
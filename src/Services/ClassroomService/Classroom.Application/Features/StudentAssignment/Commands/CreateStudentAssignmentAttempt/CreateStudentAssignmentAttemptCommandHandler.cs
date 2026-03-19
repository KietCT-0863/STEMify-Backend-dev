using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Features.StudentAssignment.Queries.GetAssignmentAttemptById;
using Classroom.Application.Specifications.StudentAssignment;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using Contracts.Abstractions.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Cloudinary;
using Shared.Helper;
using Shared.Protos.Classroom;
using Shared.Protos.Resource;

namespace Classroom.Application.Features.StudentAssignment.Commands.CreateStudentAssignmentAttempt
{
    public class CreateStudentAssignmentAttemptCommandHandler : IRequestHandler<CreateStudentAssignmentAttemptCommand, GrpcAssignmentAttemptResponse>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ILogger<CreateStudentAssignmentAttemptCommandHandler> _logger;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMediator _mediator;
        private readonly IGrpcAssignmentClient _assignmentClient;

        public CreateStudentAssignmentAttemptCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService,
            IMediator mediator,
            IGrpcAssignmentClient assignmentClient,
            ILogger<CreateStudentAssignmentAttemptCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
            _mediator = mediator;
            _assignmentClient = assignmentClient;
        }

        public async Task<GrpcAssignmentAttemptResponse> Handle(CreateStudentAssignmentAttemptCommand request, CancellationToken cancellationToken)
        {
            var spec = new GetStudentAssignmentByIdSpecification(request.StudentAssignmentId);
            var studentAssignment = await _unitOfWork.StudentAssignments.FirstOrDefaultAsync(spec, cancellationToken);
            if (studentAssignment == null)
            {
                _logger.LogWarning("StudentAssignment with Id: {Id} not found", request.StudentAssignmentId);
                throw new KeyNotFoundException($"StudentAssignment with Id {request.StudentAssignmentId} not found.");
            }

            // Get the Assignment entity to retrieve CooldownHours
            var assignment = await _assignmentClient.GetAssignmentByIdAsync(studentAssignment.AssignmentId);
            if (assignment == null)
            {
                _logger.LogWarning("Assignment with Id: {Id} not found", studentAssignment.AssignmentId);
                throw new KeyNotFoundException($"Assignment with Id {studentAssignment.AssignmentId} not found.");
            }

            // Check due date
            if (studentAssignment.DueDate.HasValue && studentAssignment.DueDate.Value < DateTime.UtcNow)
            {
                studentAssignment.Status = StudentAssignmentStatus.Expired;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException("Assignment has expired.");
            }

            var now = DateTime.UtcNow;

            // Check attempt limitations with cooldown logic
            if (studentAssignment.MaxAttemptAllowed.HasValue)
            {
                if (studentAssignment.AttemptCount < studentAssignment.MaxAttemptAllowed.Value)
                {
                    // Still have attempts available
                    await CreateAssignmentAttempt(studentAssignment, assignment, request, cancellationToken);
                }
                else if (studentAssignment.NextAttemptAvailableAt.HasValue && now >= studentAssignment.NextAttemptAvailableAt.Value)
                {
                    // Cooldown finished, allow new attempt
                    await CreateAssignmentAttempt(studentAssignment, assignment, request, cancellationToken);
                }
                else
                {
                    // Cooldown not finished or no cooldown set
                    if (studentAssignment.NextAttemptAvailableAt.HasValue)
                    {
                        var timeRemaining = studentAssignment.NextAttemptAvailableAt.Value - now;
                        _logger.LogWarning("Cooldown period active for StudentAssignmentId: {StudentAssignmentId}. Time remaining: {TimeRemaining}",
                            request.StudentAssignmentId, timeRemaining);
                        throw new InvalidOperationException(
                            $"You must wait {timeRemaining.Hours} hours and {timeRemaining.Minutes} minutes before attempting this assignment again.");
                    }
                    else
                    {
                        _logger.LogWarning("Maximum attempts reached for StudentAssignmentId: {StudentAssignmentId}", request.StudentAssignmentId);
                        throw new InvalidOperationException("Maximum attempts reached for this assignment.");
                    }
                }
            }
            else
            {
                // No attempt limit, create attempt
                await CreateAssignmentAttempt(studentAssignment, assignment, request, cancellationToken);
            }

            var assignmentAttempts = await _unitOfWork.AssignmentAttempts
                .FindAsync(a => a.StudentAssignmentId == request.StudentAssignmentId, cancellationToken);

            var lastAttempt = assignmentAttempts.OrderByDescending(a => a.AttemptNumber).FirstOrDefault();

            if (lastAttempt == null)
            {
                throw new InvalidOperationException("Failed to create assignment attempt.");
            }

            var query = new GetAssignmentAttemptByIdQuery { Id = lastAttempt.Id };
            return await _mediator.Send(query, cancellationToken);
        }

        private async Task CreateAssignmentAttempt(
            Domain.Entities.StudentAssignment studentAssignment,
            GrpcAssignmentModel assignment,
            CreateStudentAssignmentAttemptCommand request,
            CancellationToken cancellationToken)
        {
            var assignmentQuestionAttempts = new List<AssignmentQuestionAttempt>();
            if (request.AssignmentQuestionAttempts != null)
            {
                foreach (var q in request.AssignmentQuestionAttempts)
                {
                    string? answerFileUrl = null;

                    if (q.AnswerFile != null && q.AnswerFile.Length > 0)
                    {
                        var fileNameBase = $"{request.StudentAssignmentId}-{q.AssignmentQuestionId}-{Guid.NewGuid()}";
                        UploadAssetResponse? uploadResult = null;

                        if (FileTypeHelper.IsImage(q.AnswerFile))
                        {
                            var uploadRequest = new UploadImageBytesRequest
                            {
                                FileBytes = q.AnswerFile,
                                FileName = $"{fileNameBase}-image",
                            };

                            uploadResult = await _cloudinaryService.UploadImageAsync(uploadRequest);
                        }
                        else if (FileTypeHelper.IsVideo(q.AnswerFile))
                        {
                            var uploadRequest = new UploadVideoBytesRequest
                            {
                                FileBytes = q.AnswerFile,
                                FileName = $"{fileNameBase}-video",
                            };

                            uploadResult = await _cloudinaryService.UploadVideoAsync(uploadRequest);
                        }
                        else if (FileTypeHelper.IsDocument(q.AnswerFile))
                        {
                            var uploadRequest = new UploadDocumentBytesRequest
                            {
                                FileBytes = q.AnswerFile,
                                FileName = $"{fileNameBase}-doc",
                            };

                            uploadResult = await _cloudinaryService.UploadDocumentAsync(uploadRequest);
                        }
                        else
                        {
                            _logger.LogWarning("Unsupported file type for StudentAssignmentId: {StudentAssignmentId}, QuestionId: {QuestionId}",
                                request.StudentAssignmentId, q.AssignmentQuestionId);
                        }

                        if (uploadResult != null)
                        {
                            answerFileUrl = uploadResult.AssetUrl;
                        }
                    }

                    assignmentQuestionAttempts.Add(new AssignmentQuestionAttempt
                    {
                        AssignmentQuestionId = q.AssignmentQuestionId,
                        AnswerText = q.AnswerText,
                        AnswerFileUrl = answerFileUrl,
                        Points = 0
                    });
                }
            }

            var assignmentAttempt = new Domain.Entities.AssignmentAttempt
            {
                StudentAssignmentId = request.StudentAssignmentId,
                Status = Domain.Enums.AssignmentAttemptStatus.UnderReview,
                AttemptNumber = studentAssignment.AttemptCount + 1,
                TotalScore = 0,
                TeacherId = studentAssignment.StudentSectionProgress?.LessonProgress?.CourseEnrollment?.Classroom?.TeacherId.ToString() ?? string.Empty,
                AssignmentQuestionAttempts = assignmentQuestionAttempts
            };
            await _unitOfWork.AssignmentAttempts.AddAsync(assignmentAttempt, cancellationToken);

            studentAssignment.AttemptCount++;
            studentAssignment.Status = Domain.Enums.StudentAssignmentStatus.Submitted;

            // Set cooldown if reaching max attempts and cooldown is configured
            if (studentAssignment.MaxAttemptAllowed.HasValue &&
                studentAssignment.AttemptCount >= studentAssignment.MaxAttemptAllowed.Value &&
                assignment.CooldownHours.HasValue)
            {
                studentAssignment.NextAttemptAvailableAt = DateTime.UtcNow.AddHours(assignment.CooldownHours.Value);
                _logger.LogInformation("Cooldown set for StudentAssignmentId: {StudentAssignmentId} until {NextAttemptAvailableAt}",
                    studentAssignment.Id, studentAssignment.NextAttemptAvailableAt);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new assignment attempt for StudentAssignmentId: {StudentAssignmentId}, AttemptNumber: {AttemptNumber}",
                request.StudentAssignmentId, assignmentAttempt.AttemptNumber);
        }
    }
}

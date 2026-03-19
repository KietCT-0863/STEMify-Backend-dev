using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Specifications.CourseEnrollments;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.Classroom;
using Shared.Protos.Resource;

namespace Classroom.Application.Features.StudentQuiz.Queries.GetStudentQuizList
{
    public class GetStudentQuizByClassroomQueryHandler : IRequestHandler<GetStudentQuizByClassroomQuery, GrpcPagedStudentQuizzesResponse>
    {
        private readonly ILogger<GetStudentQuizByClassroomQueryHandler> _logger;
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcQuizClient _grpcQuizClient;
        private readonly IGrpcUserClient _grpcUserClient;

        public GetStudentQuizByClassroomQueryHandler(
            ILogger<GetStudentQuizByClassroomQueryHandler> logger,
            IClassroomUnitOfWork unitOfWork,
            IGrpcQuizClient quizClient,
            IGrpcUserClient grpcUserClient)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _grpcQuizClient = quizClient;
            _grpcUserClient = grpcUserClient;
        }
        public async Task<GrpcPagedStudentQuizzesResponse> Handle(GetStudentQuizByClassroomQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetCourseEnrollmentByClassroomIdSpecification(request.ClassroomId);
            var courseEnrollments = await _unitOfWork.CourseEnrollments
                .GetAllAsync(spec, cancellationToken) ?? new List<CourseEnrollment>(); ;

            // Flatten StudentQuiz
            var studentQuizzes = courseEnrollments
                .SelectMany(co => co.LessonProgress ?? [])
                .SelectMany(lp => lp.SectionProgress ?? [])
                .Select(sp => sp.StudentQuiz)
                .Where(sq => sq != null)
                .ToList();

            var grouped = studentQuizzes
                    .GroupBy(sq => sq!.QuizId)
                    .ToList();
            var quizStats = new List<GrpcQuizStatisticResponse>();

            foreach (var group in grouped)
            {
                var quizStat = await BuildQuizStatisticsAsync(group, cancellationToken);
                if (quizStat != null)
                    quizStats.Add(quizStat);
            }

            // Pagination
            var pageSize = request.PageRequest.PageSize;
            var pageNumber = request.PageRequest.PageNumber;
            var totalCount = quizStats.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedItems = quizStats
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new GrpcPagedStudentQuizzesResponse
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Items = { pagedItems }
            };
        }

        private async Task<GrpcQuizStatisticResponse?> BuildQuizStatisticsAsync(
            IGrouping<int, Domain.Entities.StudentQuiz> group,
            CancellationToken cancellationToken)
        {
            var quiz = await _grpcQuizClient.GetQuizByIdAsync(group.Key);
            if (quiz == null)
            {
                _logger.LogWarning("Quiz with ID {QuizId} not found.", group.Key);
                return null;
            }

            // Attempt cuối cùng mỗi học sinh
            var latestAttempts = group
                .Select(sq => sq!.QuizAttempts?
                    .OrderByDescending(qa => qa.CompletedAt)
                    .FirstOrDefault())
                .Where(qa => qa != null)
                .Cast<QuizAttempt>()
                .ToList();

            var submissions = latestAttempts.Count;
            var averageScore = group.Average(sq => (double?)sq!.FinalScore) ?? 0;
            var passRate = submissions == 0
                ? 0
                : (double)group.Count(sq => sq.Status == StudentQuizStatus.Passed) / submissions * 100;

            // Build chi tiết
            var studentStats = await BuildStudentStatisticsAsync(group, quiz.Questions?.ToList() ?? [], cancellationToken);
            var questionStats = BuildQuestionStatistics(latestAttempts, quiz.Questions?.ToList() ?? []);

            var quizStat = new GrpcQuizStatisticResponse
            {
                QuizId = group.Key,
                QuizName = quiz.Title,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                Submissions = submissions,
                AverageScore = (long?)averageScore,
                PassRate = (long?)passRate,
                TotalQuestions = quiz.Questions?.Count ?? 0,
            };

            quizStat.StudentStatistics.AddRange(studentStats);
            quizStat.QuestionStatistics.AddRange(questionStats);

            return quizStat;
        }

        private async Task<List<GrpcStudentStatisticResponse>> BuildStudentStatisticsAsync(
            IGrouping<int, Domain.Entities.StudentQuiz> group, List<QuestionResponse> questions, CancellationToken cancellationToken)
        {
            var tasks = group.Select(async sq =>
            {
                var student = await _grpcUserClient.GetOrganizationUserByIdAsync(Guid.Parse(sq.StudentId), cancellationToken);
                var latestAttempt = sq.QuizAttempts?
                    .OrderByDescending(qa => qa.CompletedAt)
                    .FirstOrDefault();

                var questionAttempts = latestAttempt?.QuestionAttempts ?? new List<QuizQuestionAttempt>();

                // Thống kê số câu đúng / sai / bỏ qua
                var totalCorrect = latestAttempt?.QuestionAttempts?.Count(q => q.IsCorrect) ?? 0;
                var totalSkip = questionAttempts.Count(qa =>
                    qa.AnswerAttempts == null || !qa.AnswerAttempts.Any(aa => aa.IsSelected));
                var totalIncorrect = questionAttempts.Count(qa =>
                    qa.IsCorrect == false &&
                    qa.AnswerAttempts != null &&
                    qa.AnswerAttempts.Any(aa => aa.IsSelected));
                var totalAnswers = latestAttempt?.QuestionAttempts?.Count() ?? 0;

                var questionResults = latestAttempt?.QuestionAttempts?
                    .Select(qa =>
                    {
                        var question = questions.FirstOrDefault(q => q.Id == qa.QuestionId);
                        return new GrpcQuestionResultResponse
                        {
                            QuestionId = qa.QuestionId,
                            QuestionTitle = question?.Content ?? "Unknown",
                            QuestionType = question?.QuestionType ?? "Unknown",
                            IsCorrect = qa.IsCorrect,
                            Point = (double)(qa.Score ?? 0),
                            CorrectAnswer = string.Join(", ",
                                question?.Answers?.Where(a => a.IsCorrect).Select(a => a.Content) ?? [])
                        };
                    })
                    .ToList() ?? [];

                return new GrpcStudentStatisticResponse
                {
                    StudentId = sq.StudentId,
                    StudentName = student?.FullName ?? "Unknown",
                    ImageUrl = "",
                    CompletedAt = latestAttempt?.CompletedAt?.ToString("o") ?? "",
                    TotalScore = (long?)sq.FinalScore,
                    Status = sq.Status.ToString(),
                    TotalCorrectAnswers = totalCorrect,
                    TotalIncorrectAnswers = totalIncorrect,
                    TotalSkipAnswers = totalSkip,
                    TotalAnswers = totalAnswers,
                    QuestionResults = { questionResults }
                };
            });

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        private List<GrpcQuestionStatisticResponse> BuildQuestionStatistics(
            List<QuizAttempt> latestAttempts, List<QuestionResponse> questions)
        {
            return latestAttempts
                .SelectMany(a => a!.QuestionAttempts ?? [])
                .GroupBy(qa => qa.QuestionId)
                .Select(g =>
                {
                    var question = questions.FirstOrDefault(q => q.Id == g.Key);
                    var totalCorrect = g.Count(x => x.IsCorrect);
                    var totalSkip = g.Count(x => x.AnswerAttempts == null || !x.AnswerAttempts.Any(aa => aa.IsSelected));
                    var totalIncorrect = g.Count(x =>
                        x.IsCorrect == false &&
                        x.AnswerAttempts != null &&
                        x.AnswerAttempts.Any(aa => aa.IsSelected)
                    );
                    var correctRate = g.Any() ? (double)totalCorrect / g.Count() * 100 : 0;

                    // Thống kê answer (tần suất chọn)
                    var answerStats = question?.Answers?
                        .Select(a => new GrpcAnswerStatisticResponse
                        {
                            AnswerId = a.Id,
                            Content = a.Content,
                            IsCorrect = a.IsCorrect,
                            SelectionCount = g.SelectMany(x => x.AnswerAttempts ?? [])
                                .Count(aa => aa.AnswerId == a.Id && aa.IsSelected)
                        })
                        .ToList() ?? [];

                    return new GrpcQuestionStatisticResponse
                    {
                        QuestionId = g.Key,
                        QuestionTitle = question?.Content ?? "Unknown",
                        QuestionType = question?.QuestionType ?? "Unknown",
                        TotalCorrectAnswers = totalCorrect,
                        TotalIncorrectAnswers = totalIncorrect,
                        TotalSkipAnswers = totalSkip,
                        CorrectRate = correctRate,
                        Point = (double)(question?.Points ?? 0),
                        AnswerStatistics = { answerStats }
                    };
                })
                .ToList();
        }
    }
}

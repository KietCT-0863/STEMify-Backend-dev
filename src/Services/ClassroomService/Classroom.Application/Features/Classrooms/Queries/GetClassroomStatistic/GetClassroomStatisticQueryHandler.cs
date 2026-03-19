using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Models.ClassroomModels;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using MediatR;
using Shared.Helper;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.Classrooms.Queries.GetClassroomStatistic
{
    public class GetClassroomStatisticQueryHandler :
        IRequestHandler<GetClassroomStatisticQuery, GrpcClassroomStatisticResponse>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcAssignmentClient _grpcAssignmentClient;
        private readonly IGrpcUserClient _grpcUserClient;
        private readonly IGrpcCourseClient _grpcCourseClient;

        public GetClassroomStatisticQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcAssignmentClient grpcAssignmentClient,
            IGrpcUserClient grpcUserClient,
            IGrpcCourseClient grpcCourseClient)
        {
            _unitOfWork = unitOfWork;
            _grpcAssignmentClient = grpcAssignmentClient;
            _grpcUserClient = grpcUserClient;
            _grpcCourseClient = grpcCourseClient;
        }

        public async Task<GrpcClassroomStatisticResponse> Handle(
            GetClassroomStatisticQuery request,
            CancellationToken cancellationToken)
        {
            var classroom = await _unitOfWork.Classrooms.FindByIdAsync(request.ClassroomId, cancellationToken);

            if (classroom == null)
            {
                throw new KeyNotFoundException($"Classroom with ID {request.ClassroomId} not found.");
            }
            var course = await _grpcCourseClient.GetCourseByIdAsync(classroom.CourseId);

            var spec = new Specifications.CourseEnrollments.GetCourseEnrollmentByClassroomIdSpecification(request.ClassroomId);
            var courseEnrollments = await _unitOfWork.CourseEnrollments
                .GetAllAsync(spec, cancellationToken);

            if (courseEnrollments == null || !courseEnrollments.Any())
            {
                return new GrpcClassroomStatisticResponse
                {
                    QuizStatistic = new GrpcQuizStatistic { AverageScore = 0, Submissions = 0, PassRate = 0 },
                    AssignmentStatistic = new GrpcAssignmentStatistic { AverageScore = 0, Submissions = 0, PassRate = 0 }
                };
            }

            var studentQuizzes = courseEnrollments
                .SelectMany(co => co.LessonProgress)
                .SelectMany(lp => lp.SectionProgress)
                .Select(sp => sp.StudentQuiz)
                .Where(sq => sq != null)
                .ToList();

            var studentAssignments = courseEnrollments
                .SelectMany(co => co.LessonProgress)
                .SelectMany(lp => lp.SectionProgress)
                .Select(sp => sp.StudentAssignment)
                .Where(sa => sa != null)
                .ToList();

            // Quiz statistics (classroom-level aggregate)
            var quizSubmissions = studentQuizzes.Count(sq => sq!.QuizAttempts != null && sq.QuizAttempts.Any());
            var quizAverage = studentQuizzes.Any() ? (double?)(studentQuizzes.Average(sq => (double?)sq!.FinalScore)) ?? 0 : 0;
            var quizPassCount = studentQuizzes.Count(sq => sq!.Status == StudentQuizStatus.Passed);
            var quizPassRate = quizSubmissions == 0 ? 0 : (double)quizPassCount / quizSubmissions * 100;

            // Assignment statistics (classroom-level aggregate)
            var assignmentSubmissions = studentAssignments.Count(sa => sa!.AssignmentAttempts != null && sa.AssignmentAttempts.Any());
            var assignmentAverage = studentAssignments.Any() ? (double?)(studentAssignments.Average(sa => (double?)sa!.FinalScore)) ?? 0 : 0;
            var assignmentPassCount = studentAssignments.Count(sa => sa!.Status == StudentAssignmentStatus.Passed);
            var assignmentFailedCount = studentAssignments.Count(sa => sa!.Status == StudentAssignmentStatus.Failed);
            var assignmentPassRate = assignmentSubmissions == 0 ? 0 : (double)assignmentPassCount / assignmentSubmissions * 100;
            var assignmentFailedRate = assignmentSubmissions == 0 ? 0 : (double)assignmentFailedCount / assignmentSubmissions * 100;

            // Ungraded assignments
            var ungradedStudentAssignments = studentAssignments
                .Where(sa => sa.AssignmentAttempts != null
                             && sa.AssignmentAttempts.Any(at => at.Status == AssignmentAttemptStatus.UnderReview))
                .ToList();

            var ungradedTasks = ungradedStudentAssignments.Select(async sa =>
            {
                string studentName = "Unknown";
                try
                {
                    if (!string.IsNullOrWhiteSpace(sa!.StudentId))
                    {
                        var user = await _grpcUserClient.GetOrganizationUserByIdAsync(Guid.Parse(sa.StudentId), cancellationToken);
                        studentName = user?.FullName ?? "Unknown";
                    }
                }
                catch
                {
                    studentName = "Unknown";
                }

                string assignmentTitle = "Unknown";
                try
                {
                    var assignment = await _grpcAssignmentClient.GetAssignmentByIdAsync(sa!.AssignmentId);
                    assignmentTitle = assignment?.Title ?? "Unknown";
                }
                catch
                {
                    assignmentTitle = "Unknown";
                }

                var attemptUnderReview = sa.AssignmentAttempts!
                    .Where(at => at.Status == AssignmentAttemptStatus.UnderReview)
                    .OrderByDescending(at => at.AttemptNumber)
                    .FirstOrDefault();

                return new GrpcUngradedAssignment
                {
                    StudentAssignmentId = sa!.Id,
                    StudentName = studentName,
                    AssignmentTitle = assignmentTitle,
                    AssignmentAttemptId = attemptUnderReview?.Id ?? 0
                };
            });

            var ungradedResults = await Task.WhenAll(ungradedTasks);

            var courseStat = await CalculateCourseStatisticsAsync(
                courseEnrollments,
                course,
                cancellationToken);

            var response = new GrpcClassroomStatisticResponse
            {
                QuizStatistic = new GrpcQuizStatistic
                {
                    AverageScore = quizAverage,
                    Submissions = quizSubmissions,
                    PassRate = quizPassRate
                },
                AssignmentStatistic = new GrpcAssignmentStatistic
                {
                    AverageScore = assignmentAverage,
                    Submissions = assignmentSubmissions,
                    FailedRate = assignmentFailedRate,
                    PassRate = assignmentPassRate
                },
                CourseStats = courseStat
            };

            response.UngradedAssignments.AddRange(ungradedResults);

            return response;
        }

        private async Task<GrpcCourseStatistic> CalculateCourseStatisticsAsync(
            IEnumerable<CourseEnrollment> courseEnrollments,
            CourseModel courseModel,
            CancellationToken cancellationToken)
        {
            var courseStat = new CourseStatsData
            {
                CourseId = courseModel.Id,
                CourseName = courseModel.Title,
                QuizScores = new List<double>(),
                AssignmentScores = new List<double>()
            };
            foreach (var courseEnrollment in courseEnrollments)
            {
                var quizScores = courseEnrollment.LessonProgress
                    .SelectMany(lp => lp.SectionProgress)
                    .Select(sp => sp.StudentQuiz)
                    .Where(sq => sq != null && sq.FinalScore.HasValue)
                    .Select(sq => (double)sq!.FinalScore!.Value);

                courseStat.QuizScores.AddRange(quizScores);

                var assignmentScores = courseEnrollment.LessonProgress
                    .SelectMany(lp => lp.SectionProgress)
                    .Select(sp => sp.StudentAssignment)
                    .Where(sa => sa != null && sa.FinalScore.HasValue)
                    .Select(sa => (double)sa!.FinalScore!.Value);

                courseStat.AssignmentScores.AddRange(assignmentScores);
            }

            var quizStats = StatisticsHelper.CalculateStatistics(courseStat.QuizScores);
            var grpcQuizStats = new GrpcDetailedQuizStatistic
            {
                Mean = quizStats.mean,
                Median = quizStats.median,
                Min = quizStats.min,
                Max = quizStats.max,
                Q1 = quizStats.q1,
                Q3 = quizStats.q3
            };
            grpcQuizStats.Outliers.AddRange(quizStats.outliers);

            var assignmentStats = StatisticsHelper.CalculateStatistics(courseStat.AssignmentScores);
            var grpcAssignmentStats = new GrpcDetailedAssignmentStatistic
            {
                Mean = assignmentStats.mean,
                Median = assignmentStats.median,
                Min = assignmentStats.min,
                Max = assignmentStats.max,
                Q1 = assignmentStats.q1,
                Q3 = assignmentStats.q3
            };
            grpcAssignmentStats.Outliers.AddRange(assignmentStats.outliers);

            var averageScores = courseEnrollments
            .Select(e => CalculateAverageScoreForStudent(e)) 
            .Where(avg => avg.HasValue)
            .Select(avg => avg.Value)
            .ToList();
            var bins = StatisticsHelper.BuildHistogram(averageScores);
            var StudentScoreHistogram = new GrpcStudentScoreHistogramResponse
            {
                TotalStudents = averageScores.Count,
                Bins = { bins }
            };
            var result = new GrpcCourseStatistic
            {
                Id = courseModel.Id,
                Name = courseModel.Title,
                QuizStats = grpcQuizStats,
                AssignmentStats = grpcAssignmentStats,
                StudentScoreHistogram = StudentScoreHistogram
            };

            return result;
        }

        public static double? CalculateAverageScoreForStudent(CourseEnrollment enrollment)
        {
            if (enrollment == null)
                return null;

            // Lấy điểm quiz
            var quizScores = enrollment.LessonProgress
                .SelectMany(lp => lp.SectionProgress)
                .Where(sp => sp.StudentQuiz != null 
                && sp.StudentQuiz.Status != StudentQuizStatus.Assigned && sp.StudentQuiz.Status != StudentQuizStatus.InProgress)
                .Select(sp => sp.StudentQuiz?.FinalScore ?? 0)
                .ToList();

            // Lấy điểm assignment
            var assignmentScores = enrollment.LessonProgress
                .SelectMany(lp => lp.SectionProgress)
                .Where(sp => sp.StudentAssignment != null 
                && sp.StudentAssignment.Status != StudentAssignmentStatus.Assigned && sp.StudentAssignment.Status != StudentAssignmentStatus.Submitted)
                .Select(sp => sp.StudentAssignment?.FinalScore ?? 0)
                .ToList();

            // Không có điểm => Không tính average
            if (quizScores.Count == 0 && assignmentScores.Count == 0)
                return null;

            double? quizAvg = quizScores.Count > 0 ? (double)quizScores.Average() : (double?)null;
            double? assignmentAvg = assignmentScores.Count > 0 ? (double)assignmentScores.Average() : (double?)null;

            // Nếu có cả quiz & assignment
            if (quizAvg.HasValue && assignmentAvg.HasValue)
                return (quizAvg.Value + assignmentAvg.Value) / 2.0;

            // Nếu chỉ có quiz
            if (quizAvg.HasValue)
                return quizAvg.Value;

            // Nếu chỉ có assignment
            return assignmentAvg!.Value;
        }

    }
}
 
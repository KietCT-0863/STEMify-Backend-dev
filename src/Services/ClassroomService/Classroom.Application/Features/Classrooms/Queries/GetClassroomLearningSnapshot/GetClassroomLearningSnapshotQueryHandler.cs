using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Specifications.Classrooms;
using Classroom.Application.Specifications.CourseEnrollments;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Protos.Classroom;
using Shared.Protos.Resource;
using System.Linq;

namespace Classroom.Application.Features.Classrooms.Queries.GetClassroomLearningSnapshot
{
    public class GetClassroomLearningSnapshotQueryHandler
        : IRequestHandler<GetClassroomLearningSnapshotQuery, GrpcClassroomLearningSnapshotResponse>
    {
        private readonly ILogger<GetClassroomLearningSnapshotQueryHandler> _logger;
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IUserCacheService _userCache;
        private readonly IGrpcQuizClient _grpcQuizClient;
        private readonly IGrpcAssignmentClient _grpcAssignmentClient;
        private readonly IGrpcRubricCriterionClient _grpcRubricCriterionClient;
        private readonly IGrpcSectionClient _grpcSectionClient;
        private readonly IGrpcCurriculumClient _grpcCurriculumClient;
        private readonly IGrpcCourseClient _grpcCourseClient;

        public GetClassroomLearningSnapshotQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            IUserCacheService userCache,
            IGrpcQuizClient grpcQuizClient,
            IGrpcAssignmentClient grpcAssignmentClient,
            IGrpcRubricCriterionClient grpcRubricCriterionClient,
            IGrpcSectionClient grpcSectionClient,
            IGrpcCurriculumClient grpcCurriculumClient,
            IGrpcCourseClient grpcCourseClient,
            ILogger<GetClassroomLearningSnapshotQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _userCache = userCache;
            _grpcQuizClient = grpcQuizClient;
            _grpcAssignmentClient = grpcAssignmentClient;
            _grpcRubricCriterionClient = grpcRubricCriterionClient;
            _grpcSectionClient = grpcSectionClient;
            _grpcCurriculumClient = grpcCurriculumClient;
            _grpcCourseClient = grpcCourseClient;
            _logger = logger;
        }

        public async Task<GrpcClassroomLearningSnapshotResponse> Handle(
            GetClassroomLearningSnapshotQuery request,
            CancellationToken cancellationToken)
        {
            // Get classroom
            var classroom = await _unitOfWork.Classrooms.FirstOrDefaultAsync(
                new ClassroomByIdSpecification(request.ClassroomId),
                cancellationToken);

            if (classroom == null)
            {
                throw new NotFoundException($"Classroom with ID {request.ClassroomId} not found.");
            }

            // Calculate analysis period
            var daysBack = request.DaysBack ?? 7;
            var toDate = DateTime.UtcNow;
            var fromDate = toDate.AddDays(-daysBack);

            // Get course enrollments
            var courseEnrollmentSpec = new GetCourseEnrollmentByClassroomIdSpecification(request.ClassroomId);
            var courseEnrollments = await _unitOfWork.CourseEnrollments
                .GetAllAsync(courseEnrollmentSpec, cancellationToken);

            // Filter by student if specified
            if (!string.IsNullOrWhiteSpace(request.StudentId))
            {
                var studentGuid = Guid.Parse(request.StudentId);
                courseEnrollments = courseEnrollments
                    .Where(ce => ce.StudentId == studentGuid)
                    .ToList();
            }

            // Get curriculum enrollments
            var curriculumEnrollments = courseEnrollments
                .Where(ce => ce.CurriculumEnrollmentId.HasValue)
                .Select(ce => ce.CurriculumEnrollment)
                .Where(ce => ce != null)
                .ToList();

            var response = new GrpcClassroomLearningSnapshotResponse
            {
                Classroom = new GrpcClassroomBasicInfo
                {
                    Id = classroom.Id,
                    Name = classroom.Name
                },
                AnalysisPeriod = new GrpcAnalysisPeriod
                {
                    FromDate = fromDate.ToString("o"),
                    ToDate = toDate.ToString("o"),
                    DaysBack = daysBack
                }
            };

            // Map students
            var studentTasks = courseEnrollments.Select(async ce =>
            {
                var user = await _userCache.GetOrganizationUserByIdAsync(ce.StudentId, cancellationToken);
                return new GrpcStudentLearningData
                {
                    StudentId = ce.StudentId.ToString(),
                    StudentName = user?.Name ?? "Unknown",
                    JoinedAt = ce.EnrolledAt.ToString("o")
                };
            });

            var students = await Task.WhenAll(studentTasks);
            response.Students.AddRange(students);

            // Map enrollments
            var curriculumCache = new Dictionary<int, string>();
            var courseCache = new Dictionary<int, string>();
            foreach (var student in students)
            {
                var studentGuid = Guid.Parse(student.StudentId);
                var courseEnrollment = courseEnrollments.FirstOrDefault(ce => ce.StudentId == studentGuid);
                if (courseEnrollment != null)
                {
                    // Course enrollment
                    if (!courseCache.ContainsKey(courseEnrollment.CourseId))
                    {
                        var course = await _grpcCourseClient.GetCourseByIdAsync(courseEnrollment.CourseId);
                        courseCache[courseEnrollment.CourseId] = course?.Title ?? string.Empty;
                    }
                    student.Enrollments.Add(new GrpcEnrollmentData
                    {
                        StudentId = student.StudentId,
                        ProgressPercentage = courseEnrollment.ProgressPercentage,
                        EnrollmentType =  "course",
                        CourseId = courseEnrollment.CourseId,
                        CourseName = courseCache[courseEnrollment.CourseId],
                        Status = courseEnrollment.Status.ToString()
                    });

                    // Curriculum enrollment
                    if (courseEnrollment.CurriculumEnrollmentId.HasValue && courseEnrollment.CurriculumEnrollment != null)
                    {
                        var curriculumId = courseEnrollment.CurriculumEnrollment.CurriculumId;
                        if (!curriculumCache.ContainsKey(curriculumId))
                        {
                            var curriculum = await _grpcCurriculumClient.GetCurriculumByIdAsync(curriculumId);
                            curriculumCache[curriculumId] = curriculum?.Title ?? string.Empty;
                        }

                        var enrollmentData = new GrpcEnrollmentData
                        {
                            StudentId = student.StudentId,
                            ProgressPercentage = courseEnrollment.CurriculumEnrollment.ProgressPercentage,
                            EnrollmentType = "curriculum",
                            CurriculumId = curriculumId,
                            Status = courseEnrollment.CurriculumEnrollment.Status.ToString()
                        };
                        var curriculumName = curriculumCache[curriculumId];
                        if (!string.IsNullOrWhiteSpace(curriculumName))
                        {
                            enrollmentData.CurriculumName = curriculumName;
                        }
                        student.Enrollments.Add(enrollmentData);
                    }
                }
            }

            // Get all student quizzes and quiz attempts
            var allStudentQuizzes = courseEnrollments
                .SelectMany(ce => ce.LessonProgress)
                .SelectMany(lp => lp.SectionProgress)
                .Where(sp => sp.StudentQuiz != null)
                .Select(sp => sp.StudentQuiz!)
                .ToList();

            // Filter by date if needed
            var filteredStudentQuizzes = allStudentQuizzes
                .Where(sq => sq.AssignedAt >= fromDate || sq.QuizAttempts.Any(qa => qa.StartedAt >= fromDate))
                .ToList();

            // Map student quizzes
            var quizDetailsCache = new Dictionary<int, QuizResponse?>();
            foreach (var studentQuiz in filteredStudentQuizzes)
            {
                if (!quizDetailsCache.ContainsKey(studentQuiz.QuizId))
                {
                    quizDetailsCache[studentQuiz.QuizId] = await _grpcQuizClient.GetQuizByIdAsync(studentQuiz.QuizId);
                }

                var quizDetail = quizDetailsCache[studentQuiz.QuizId];
                var questionLookup = quizDetail?.Questions?.ToDictionary(q => q.Id) ?? new Dictionary<int, QuestionResponse>();
                var studentQuizData = new GrpcStudentQuizData
                {
                    Id = studentQuiz.Id,
                    StudentId = studentQuiz.StudentId,
                    QuizTitle = quizDetail?.Title ?? "Unknown Quiz",
                    AttemptCount = studentQuiz.AttemptCount,
                    FinalScore = (double)(studentQuiz.FinalScore ?? 0),
                    Status = studentQuiz.Status.ToString(),
                    AssignedAt = studentQuiz.AssignedAt.ToString("o"),
                    MaxAttemptAllowed = studentQuiz.MaxAttemptAllowed.HasValue
                        ? studentQuiz.MaxAttemptAllowed.Value
                        : (int?)null
                };
                var completedAt = studentQuiz.QuizAttempts
                    .OrderByDescending(qa => qa.CompletedAt)
                    .FirstOrDefault()?.CompletedAt;
                if (completedAt.HasValue)
                {
                    studentQuizData.CompletedAt = completedAt.Value.ToString("o");
                }
                if (!string.IsNullOrEmpty(quizDetail?.Description))
                {
                    studentQuizData.QuizDescription =  quizDetail.Description;
                }
                response.StudentQuizzes.Add(studentQuizData);

                // Map quiz attempts
                var filteredAttempts = studentQuiz.QuizAttempts
                    .Where(qa => qa.StartedAt >= fromDate)
                    .OrderBy(qa => qa.AttemptNumber)
                    .ToList();

                foreach (var attempt in filteredAttempts)
                {
                    var timeSpent = attempt.CompletedAt.HasValue
                        ? (attempt.CompletedAt.Value - attempt.StartedAt).TotalMinutes
                        : 0;

                    var attemptData = new GrpcQuizAttemptData
                    {
                        StudentQuizId = studentQuiz.Id,
                        AttemptNumber = attempt.AttemptNumber,
                        TotalScore = (double)attempt.TotalScore,
                        Status = attempt.Status.ToString(),
                        TimeSpentMinutes = timeSpent,
                        StartedAt = attempt.StartedAt.ToString("o")
                    };
                    if (attempt.CompletedAt.HasValue)
                    {
                        attemptData.CompletedAt = attempt.CompletedAt.Value.ToString("o");
                    }

                    // Map question attempts
                    foreach (var questionAttempt in attempt.QuestionAttempts)
                    {
                        questionLookup.TryGetValue(questionAttempt.QuestionId, out var questionDetail);
                        var selectedAnswerContents = new List<string>();
                        if (questionDetail != null)
                        {
                            foreach (var answerAttempt in questionAttempt.AnswerAttempts)
                            {
                                var answerDetail = questionDetail.Answers.FirstOrDefault(a => a.Id == answerAttempt.AnswerId);
                                if (answerAttempt.IsSelected && answerDetail != null && !string.IsNullOrWhiteSpace(answerDetail.Content))
                                {
                                    selectedAnswerContents.Add(answerDetail.Content);
                                }
                            }
                        }

                        var questionData = new GrpcQuestionAttemptData
                        {
                            QuestionId = questionAttempt.QuestionId,
                            IsCorrect = questionAttempt.IsCorrect,
                            QuestionType = questionDetail?.QuestionType ?? "Unknown",
                            QuestionContent = questionDetail?.Content ?? string.Empty,
                        };
                        questionData.Topics.AddRange(new List<string>());
                        if (selectedAnswerContents.Any())
                        {
                            questionData.AnswerContent = string.Join("; ", selectedAnswerContents);
                            questionData.IsSelected = true;
                        }

                        // Map answer attempts
                        foreach (var answerAttempt in questionAttempt.AnswerAttempts)
                        {
                            if (answerAttempt.IsSelected)
                            {
                                questionData.IsSelected = true;
                                if (string.IsNullOrEmpty(questionData.AnswerContent))
                                {
                                    var ansContent = questionDetail?.Answers
                                        .FirstOrDefault(a => a.Id == answerAttempt.AnswerId)?.Content;
                                    if (!string.IsNullOrWhiteSpace(ansContent))
                                    {
                                        questionData.AnswerContent = ansContent;
                                    }
                                }
                            }
                        }

                        attemptData.QuestionAttempts.Add(questionData);
                    }

                    response.QuizAttempts.Add(attemptData);
                }
            }

            // Get all student assignments and assignment attempts
            var allStudentAssignments = courseEnrollments
                .SelectMany(ce => ce.LessonProgress)
                .SelectMany(lp => lp.SectionProgress)
                .Where(sp => sp.StudentAssignment != null)
                .Select(sp => sp.StudentAssignment!)
                .ToList();

            // Filter by date
            var filteredStudentAssignments = allStudentAssignments
                .Where(sa => sa.AssignedAt >= fromDate || sa.AssignmentAttempts.Any(aa => aa.SubmittedAt >= fromDate))
                .ToList();

            // Map student assignments
            var assignmentDetailsCache = new Dictionary<int, GrpcAssignmentModel?>();
            var rubricCriterionCache = new Dictionary<int, RubricCriterionResponse?>();

            foreach (var studentAssignment in filteredStudentAssignments)
            {
                if (!assignmentDetailsCache.ContainsKey(studentAssignment.AssignmentId))
                {
                    assignmentDetailsCache[studentAssignment.AssignmentId] = await _grpcAssignmentClient.GetAssignmentByIdAsync(studentAssignment.AssignmentId);
                }

                var assignmentDetail = assignmentDetailsCache[studentAssignment.AssignmentId];
                var assignmentData = new GrpcStudentAssignmentData
                {
                    StudentId = studentAssignment.StudentId,
                    FinalScore = (double)(studentAssignment.FinalScore ?? 0),
                    SubmissionCount = studentAssignment.AttemptCount
                };
                var lastAttempt = studentAssignment.AssignmentAttempts
                    .OrderByDescending(aa => aa.SubmittedAt)
                    .FirstOrDefault();
                if (lastAttempt != null)
                {
                    assignmentData.SubmittedAt =  lastAttempt.SubmittedAt.ToString("o");
                }
                if (studentAssignment.DueDate.HasValue)
                {
                    assignmentData.DueDate =studentAssignment.DueDate.Value.ToString("o");
                }

                // Map assignment question attempts
                var filteredAttempts = studentAssignment.AssignmentAttempts
                    .Where(aa => aa.SubmittedAt >= fromDate)
                    .OrderBy(aa => aa.AttemptNumber)
                    .ToList();

                foreach (var attempt in filteredAttempts)
                {
                    foreach (var questionAttempt in attempt.AssignmentQuestionAttempts)
                    {
                        var questionData = new GrpcAssignmentQuestionAttemptData
                        {
                            QuestionId = questionAttempt.AssignmentQuestionId,
                            AnswerText = questionAttempt.AnswerText ?? string.Empty,
                            Points = (double)questionAttempt.Points,
                        };
                        questionData.Topics.AddRange(new List<string>());
                        if (!string.IsNullOrEmpty(attempt.Feedback))
                        {
                            questionData.Feedback =  attempt.Feedback ;
                        }

                        questionData.QuestionContent = string.Empty;

                        // Map rubric scores
                        foreach (var rubricScore in questionAttempt.RubricScores)
                        {
                            if (!rubricCriterionCache.ContainsKey(rubricScore.RubricCriterionId))
                            {
                                rubricCriterionCache[rubricScore.RubricCriterionId] = await _grpcRubricCriterionClient.GetRubricCriterionByIdAsync(rubricScore.RubricCriterionId);
                            }

                            var criterion = rubricCriterionCache[rubricScore.RubricCriterionId];
                            var rubricData = new GrpcRubricScoreData
                            {
                                Id = rubricScore.Id,
                                RubricCriterionId = rubricScore.RubricCriterionId,
                                CriterionName = criterion?.CriterionName ?? string.Empty,
                                MaxPoints = criterion?.MaxPoints ?? 0,
                                Points = (double)rubricScore.Points
                            };
                            if (criterion?.Description != null)
                            {
                                rubricData.CriterionDescription = criterion.Description;
                            }
                            questionData.RubricScores.Add(rubricData);
                        }

                        assignmentData.QuestionAttempts.Add(questionData);
                    }
                }

                response.StudentAssignments.Add(assignmentData);
            }

            // Map engagement metrics & per-student progress summaries
            foreach (var student in students)
            {
                var studentGuid = Guid.Parse(student.StudentId);

                // --- Assessment-based metrics (quizzes + assignments) ---
                var studentQuizCount = filteredStudentQuizzes
                    .Count(sq => sq.StudentId == studentGuid.ToString());
                var completedStudentQuizzes = filteredStudentQuizzes
                    .Count(sq => sq.StudentId == studentGuid.ToString() && sq.Status.ToString() == "Passed");

                var studentAssignmentCount = filteredStudentAssignments
                    .Count(sa => sa.StudentId == studentGuid.ToString());
                var completedStudentAssignments = filteredStudentAssignments
                    .Count(sa => sa.StudentId == studentGuid.ToString() && sa.Status.ToString() == "Passed");

                var totalAssessments = studentQuizCount + studentAssignmentCount;
                var completedAssessments = completedStudentQuizzes + completedStudentAssignments;
                var assessmentCompletionRate = totalAssessments > 0
                    ? (double)completedAssessments / totalAssessments
                    : 0d;

                // Calculate days since last activity
                var lastQuizActivity = filteredStudentQuizzes
                    .Where(sq => sq.StudentId == studentGuid.ToString())
                    .SelectMany(sq => sq.QuizAttempts)
                    .OrderByDescending(qa => qa.StartedAt)
                    .FirstOrDefault()?.StartedAt;

                var lastAssignmentActivity = filteredStudentAssignments
                    .Where(sa => sa.StudentId == studentGuid.ToString())
                    .SelectMany(sa => sa.AssignmentAttempts)
                    .OrderByDescending(aa => aa.SubmittedAt)
                    .FirstOrDefault()?.SubmittedAt;

                var lastActivity = new[] { lastQuizActivity, lastAssignmentActivity }
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();

                var daysSinceLastActivity = lastActivity != DateTime.MinValue
                    ? (int)(toDate - lastActivity).TotalDays
                    : int.MaxValue;

                // Calculate active days (simplified - count days with any activity)
                var activeDays = filteredStudentQuizzes
                    .Where(sq => sq.StudentId == studentGuid.ToString())
                    .SelectMany(sq => sq.QuizAttempts)
                    .Select(qa => qa.StartedAt.Date)
                    .Concat(filteredStudentAssignments
                        .Where(sa => sa.StudentId == studentGuid.ToString())
                        .SelectMany(sa => sa.AssignmentAttempts)
                        .Select(aa => aa.SubmittedAt.Date))
                    .Distinct()
                    .Count(d => d >= fromDate.Date && d <= toDate.Date);

                var attemptDurations = filteredStudentQuizzes
                    .Where(sq => sq.StudentId == studentGuid.ToString())
                    .SelectMany(sq => sq.QuizAttempts)
                    .Select(qa =>
                    {
                        if (qa.CompletedAt.HasValue)
                        {
                            return (qa.CompletedAt.Value - qa.StartedAt).TotalMinutes;
                        }
                        return 0d;
                    })
                    .Where(d => d > 0)
                    .ToList();
                var avgSessionDuration = attemptDurations.Any()
                    ? attemptDurations.Average()
                    : 0d;

                // Content / section progress (reading + activities)
                var studentSectionProgress = courseEnrollments
                    .Where(ce => ce.StudentId == studentGuid)
                    .SelectMany(ce => ce.LessonProgress)
                    .SelectMany(lp => lp.SectionProgress)
                    .ToList();

                var totalSections = studentSectionProgress.Count;
                var completedSections = studentSectionProgress
                    .Count(sp => sp.Status.ToString() == "Completed");

                var contentCompletionRate = totalSections > 0
                    ? (double)completedSections / totalSections
                    : 0d;

                var engagementMetric = new GrpcEngagementMetricData
                {
                    StudentId = student.StudentId,
                    // Keep engagement completion_rate aligned with assessment-based view,
                    // as UI and downstream consumers already rely on this meaning.
                    CompletionRate = assessmentCompletionRate,
                    DaysSinceLastActivity = daysSinceLastActivity,
                    ActiveDaysLast7Days = activeDays,
                    AvgSessionDurationMinutes = avgSessionDuration
                };

                response.EngagementMetrics.Add(engagementMetric);

                // Compact per-student progress summary to avoid scanning
                // deeply nested event-level records on the AI side.
                var progressSummary = new GrpcStudentProgressSummary
                {
                    StudentId = student.StudentId,
                    AssessmentCompletionRate = assessmentCompletionRate,
                    TotalAssessments = totalAssessments,
                    CompletedAssessments = completedAssessments,
                    ContentCompletionRate = contentCompletionRate,
                    TotalSections = totalSections,
                    CompletedSections = completedSections
                };

                response.StudentProgressSummaries.Add(progressSummary);
            }

            // Map section progress
            var sectionCache = new Dictionary<int, SectionResponse?>();

            foreach (var courseEnrollment in courseEnrollments)
            {
                foreach (var lessonProgress in courseEnrollment.LessonProgress)
                {
                    foreach (var sectionProgress in lessonProgress.SectionProgress)
                    {
                        SectionResponse? sectionDetail = null;
                        if (!sectionCache.TryGetValue(sectionProgress.SectionId, out sectionDetail))
                        {
                            sectionDetail = await _grpcSectionClient.GetSectionByIdAsync(sectionProgress.SectionId);
                            sectionCache[sectionProgress.SectionId] = sectionDetail;
                        }

                        var progressData = new GrpcSectionProgressData
                        {
                            StudentId = courseEnrollment.StudentId.ToString(),
                            SectionId = sectionProgress.SectionId,
                            SectionName = sectionDetail?.Title ?? string.Empty,
                            Status = sectionProgress.Status.ToString()
                        };
                        // Use CompletedAt as last activity, or calculate from quiz/assignment attempts
                        var lastActivity = sectionProgress.CompletedAt;
                        if (!lastActivity.HasValue)
                        {
                            var quizActivity = sectionProgress.StudentQuiz?.QuizAttempts
                                .OrderByDescending(qa => qa.StartedAt)
                                .FirstOrDefault()?.StartedAt;
                            var assignmentActivity = sectionProgress.StudentAssignment?.AssignmentAttempts
                                .OrderByDescending(aa => aa.SubmittedAt)
                                .FirstOrDefault()?.SubmittedAt;
                            lastActivity = new[] { quizActivity, assignmentActivity }
                                .Where(d => d.HasValue)
                                .Select(d => d!.Value)
                                .DefaultIfEmpty(DateTime.MinValue)
                                .Max();
                            if (lastActivity == DateTime.MinValue) lastActivity = null;
                        }
                        if (lastActivity.HasValue)
                        {
                            progressData.LastActivityAt = lastActivity.Value.ToString("o");
                        }
                        response.SectionProgress.Add(progressData);
                    }
                }
            }

            // Build a minimal topics catalog from section metadata and quiz titles
            var topicCatalog = new List<GrpcTopicCatalogItem>();
            foreach (var sectionEntry in sectionCache)
            {
                var sectionDetail = sectionEntry.Value;
                if (sectionDetail == null)
                {
                    continue;
                }

                var contentItems = new List<GrpcContentItem>();
                foreach (var quizId in sectionDetail.QuizIds)
                {
                    if (!quizDetailsCache.ContainsKey(quizId))
                    {
                        quizDetailsCache[quizId] = await _grpcQuizClient.GetQuizByIdAsync(quizId);
                    }

                    var quizDetail = quizDetailsCache[quizId];
                    if (quizDetail != null)
                    {
                        contentItems.Add(new GrpcContentItem
                        {
                            ContentType = "Quiz",
                            ContentTitle = quizDetail.Title
                        });
                    }
                }

                var sectionCatalog = new GrpcSectionCatalogData
                {
                SectionId = sectionDetail.Id,
                SectionTitle = sectionDetail.Title
                };
                sectionCatalog.Contents.AddRange(contentItems);

                var topicItem = new GrpcTopicCatalogItem
                {
                    TopicId = sectionEntry.Key,
                    TopicName = sectionDetail.Title
                };
                topicItem.Sections.Add(sectionCatalog);

                topicCatalog.Add(topicItem);
            }

            response.TopicsCatalog.AddRange(topicCatalog);

            _logger.LogInformation(
                "Generated learning snapshot for classroom {ClassroomId} with {StudentCount} students, {QuizCount} quizzes, {AssignmentCount} assignments",
                request.ClassroomId,
                response.Students.Count,
                response.StudentQuizzes.Count,
                response.StudentAssignments.Count);

            return response;
        }
    }
}


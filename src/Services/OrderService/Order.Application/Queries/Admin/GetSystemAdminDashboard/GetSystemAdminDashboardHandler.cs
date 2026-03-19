using MediatR;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Grpc;
using Order.Application.Models;
using Shared.Protos.Classroom;
using Shared.Protos.Order;

namespace Order.Application.Queries.Admin.GetSystemAdminDashboard
{
    public class GetSystemAdminDashboardQueryHandler
        : IRequestHandler<GetSystemAdminDashboardQuery, GetSystemAdminDashboardResponse>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IGrpcClassroomClient _classroomClient;
        private readonly IGrpcCourseEnrollmentClient _courseEnrollmentClient;
        private readonly IGrpcCertificateClient _certificateClient;
        private readonly IGrpcQuizAttemptClient _quizAttemptClient;
        private readonly IGrpcAssignmentAttemptClient _assignmentAttemptClient;

        public GetSystemAdminDashboardQueryHandler(
            IOrderUnitOfWork unitOfWork,
            IGrpcClassroomClient classroomClient,
            IGrpcCourseEnrollmentClient courseEnrollmentClient,
            IGrpcCertificateClient certificateClient,
            IGrpcAssignmentAttemptClient assignmentAttemptClient,
            IGrpcQuizAttemptClient quizAttemptClient)
        {
            _unitOfWork = unitOfWork;
            _classroomClient = classroomClient;
            _courseEnrollmentClient = courseEnrollmentClient;
            _certificateClient = certificateClient;
            _quizAttemptClient = quizAttemptClient;
            _assignmentAttemptClient = assignmentAttemptClient;
        }

        public async Task<GetSystemAdminDashboardResponse> Handle(
            GetSystemAdminDashboardQuery request,
            CancellationToken cancellationToken)
        {
            var period = request.Period ?? "Month";
            var (currentPeriodStart, currentPeriodEnd) = GetPeriodDateRange(period, DateTime.UtcNow);

            var (previousPeriodStart, previousPeriodEnd) = GetPreviousPeriodDateRange(
                period,
                currentPeriodStart);

            // Fetch all organizations
            var allOrganizations = await _unitOfWork.Organizations.GetAllAsync(
                cancellationToken: cancellationToken);

            var activeOrganizations = allOrganizations
                .Where(o => o.Status.ToString() == "Active")
                .ToList();

            // Get current period data
            var currentData = await GetSystemAdminStatistics(
                currentPeriodStart,
                currentPeriodEnd,
                cancellationToken);

            // Get previous period data for comparison
            var previousData = await GetSystemAdminStatistics(
                previousPeriodStart,
                previousPeriodEnd,
                cancellationToken);

            // Calculate period comparison
            var periodComparison = CalculatePeriodComparison(currentData, previousData);

            // Get top courses
            var topCourses = await GetTopCourses(
                currentPeriodStart,
                currentPeriodEnd,
                cancellationToken);

            // Get top organizations
            var topOrganizations = await GetTopOrganizations(
                currentPeriodStart,
                currentPeriodEnd,
                cancellationToken);

            var response = new GetSystemAdminDashboardResponse
            {
                Summary = new SystemAdminSummary
                {
                    TotalOrganizations = allOrganizations.Count,
                    ActiveOrganizations = activeOrganizations.Count,
                    TotalEnrollments = currentData.TotalEnrollments,
                    TotalStudents = currentData.TotalStudents,
                    TotalTeachers = currentData.TotalTeachers,
                    TotalClassrooms = currentData.TotalClassrooms,
                    TotalCertificates = currentData.TotalCertificates,
                    OverallPassRate = currentData.OverallPassRate
                },
                Subscriptions = currentData.SubscriptionStats,
                Enrollments = currentData.EnrollmentStats,
                PeriodComparison = periodComparison
            };

            response.TopCourses.AddRange(topCourses);
            response.TopOrganizations.AddRange(topOrganizations);

            return response;
        }

        private async Task<SystemAdminAggregateData> GetSystemAdminStatistics(
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken cancellationToken)
        {
            var startOffset = new DateTimeOffset(periodStart.ToUniversalTime(), TimeSpan.Zero);
            var endOffset = new DateTimeOffset(periodEnd.ToUniversalTime(), TimeSpan.Zero);

            // Get all subscription orders in period
            var subscriptionOrders = await _unitOfWork.OrganizationSubscriptionOrders
                .FindAsync(
                    predicate: x => x.StartDate >= startOffset && x.StartDate < endOffset,
                    cancellationToken: cancellationToken);

            var organizations = await _unitOfWork.Organizations
                .FindAsync(
                    predicate: x => x.CreatedDate >= startOffset && x.CreatedDate < endOffset,
                    cancellationToken: cancellationToken);

            var subscriptionOrderIds = subscriptionOrders.Select(x => x.Id).ToList();

            // Get license assignments
            var periodStartUtc = DateTime.SpecifyKind(periodStart, DateTimeKind.Utc);
            var periodEndUtc = DateTime.SpecifyKind(periodEnd, DateTimeKind.Utc);

            var licenseAssignments = await _unitOfWork.LicenseAssignments
                .FindAsync(
                    predicate: x => subscriptionOrderIds.Contains(x.OrganizationSubscriptionOrderId)
                        && x.AssignedAt >= periodStartUtc
                        && x.AssignedAt < periodEndUtc,
                    cancellationToken: cancellationToken);

            var totalStudents = licenseAssignments
                .Where(x => x.LicenseType.ToString() == "Student")
                .Select(x => x.OrganizationUserId)
                .Distinct()
                .Count();

            var totalTeachers = licenseAssignments
                .Where(x => x.LicenseType.ToString() == "Teacher")
                .Select(x => x.OrganizationUserId)
                .Distinct()
                .Count();

            // Get all classrooms and enrollments
            var allClassrooms = new List<GrpcClassroomResponse>();
            var allCourseEnrollments = new List<GrpcCourseEnrollmentModel>();
            var allCertificates = new List<GrpcCertificateModel>();

            foreach (var subscriptionOrderId in subscriptionOrderIds)
            {
                var getClassroomsRequest = new GetClassroomsRequest
                {
                    PageNumber = 1,
                    PageSize = 1000,
                    FromDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodStart.ToUniversalTime()),
                    ToDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodEnd.ToUniversalTime()),
                    OrganizationSubscriptionOrderId = subscriptionOrderId
                };

                var classroomsResponse = await _classroomClient.GetPagedClassrooms(getClassroomsRequest);
                allClassrooms.AddRange(classroomsResponse.Items);

                foreach (var classroom in classroomsResponse.Items)
                {
                    var enrollmentsRequest = new GetCourseEnrollmentsRequest
                    {
                        PageNumber = 1,
                        PageSize = 1000,
                        ClassroomId = classroom.Id,
                    };

                    var enrollmentsResponse = await _courseEnrollmentClient.GetPagedCourseEnrollments(enrollmentsRequest);
                    allCourseEnrollments.AddRange(enrollmentsResponse.Items);

                    var enrollmentIds = enrollmentsResponse.Items.Select(x => x.Id).ToList();
                    foreach (var enrollmentId in enrollmentIds)
                    {
                        var certificatesRequest = new GetCertificatesRequest
                        {
                            PageNumber = 1,
                            PageSize = 1000,
                            CurriculumEnrollmentId = enrollmentId
                        };

                        var certificatesResponse = await _certificateClient.GetPagedCertificates(certificatesRequest);
                        allCertificates.AddRange(certificatesResponse.Items);
                    }
                }
            }

            // Calculate subscription stats
            var subscriptionStats = CalculateSubscriptionStats(subscriptionOrders);

            // Calculate enrollment stats
            var enrollmentStats = CalculateEnrollmentStats(allCourseEnrollments, periodStart, periodEnd);

            var completedEnrollments = allCourseEnrollments
                .Where(x => x.Status == "Completed")
                .Count();

            var passRate = allCourseEnrollments.Count > 0
                ? (double)completedEnrollments / allCourseEnrollments.Count * 100
                : 0;

            return new SystemAdminAggregateData
            {
                TotalEnrollments = allCourseEnrollments.Count,
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers,
                TotalClassrooms = allClassrooms.Count,
                TotalCertificates = allCertificates.Count,
                TotalOrganizations = organizations.Count,
                OverallPassRate = passRate,
                SubscriptionStats = subscriptionStats,
                EnrollmentStats = enrollmentStats,
                TotalRevenue = subscriptionOrders.Sum(x => (double)x.NetAmount)
            };
        }

        private SystemAdminSubscriptionStats CalculateSubscriptionStats(
            IEnumerable<Domain.Entities.OrganizationSubscriptionOrder> subscriptionOrders)
        {
            var activeSubscriptions = subscriptionOrders
                .Where(x => x.Status.ToString() == "Active")
                .ToList();

            var expiredSubscriptions = subscriptionOrders
                .Where(x => x.Status.ToString() == "Expired")
                .ToList();

            var byPlan = subscriptionOrders
                .GroupBy(x => x.PlanName)
                .Select(g => new SubscriptionByPlanModel
                {
                    PlanName = g.Key,
                    Count = g.Count(),
                    ActiveCount = g.Count(x => x.Status.ToString() == "Active"),
                    Revenue = g.Sum(x => (double)x.NetAmount)
                })
                .ToList();

            var response = new SystemAdminSubscriptionStats
            {
                TotalSubscriptions = subscriptionOrders.Count(),
                ActiveSubscriptions = activeSubscriptions.Count,
                ExpiredSubscriptions = expiredSubscriptions.Count,
                TotalRevenue = subscriptionOrders.Sum(x => (double)x.NetAmount)
            };

            response.ByPlan.AddRange(byPlan);

            return response;
        }

        private SystemAdminEnrollmentStats CalculateEnrollmentStats(
            List<GrpcCourseEnrollmentModel> enrollments,
            DateTime periodStart,
            DateTime periodEnd)
        {
            var completedEnrollments = enrollments
                .Where(x => x.Status == "Completed")
                .Count();

            var inProgressEnrollments = enrollments
                .Where(x => x.Status == "InProgress")
                .Count();

            var completionRate = enrollments.Count > 0
                ? (double)completedEnrollments / enrollments.Count * 100
                : 0;

            // Group by month
            var enrollmentsByMonth = enrollments
                .GroupBy(e => new DateTime(
                    e.EnrolledAt.ToDateTime().Year,
                    e.EnrolledAt.ToDateTime().Month,
                    1))
                .Select(g => new MonthlyEnrollmentModel
                {
                    Month = g.Key.ToString("yyyy-MM"),
                    Count = g.Count(),
                    Completed = g.Count(x => x.Status == "Completed")
                })
                .OrderBy(m => m.Month)
                .ToList();

            var stats = new SystemAdminEnrollmentStats
            {
                TotalEnrollments = enrollments.Count,
                CompletedEnrollments = completedEnrollments,
                InProgressEnrollments = inProgressEnrollments,
                CompletionRate = completionRate
            };

            stats.EnrollmentsByMonth.AddRange(enrollmentsByMonth);

            return stats;
        }

        private async Task<List<TopCourseModel>> GetTopCourses(
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken cancellationToken)
        {
            // Get all classrooms in the period
            var getClassroomsRequest = new GetClassroomsRequest
            {
                PageNumber = 1,
                PageSize = 10000,
                FromDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodStart.ToUniversalTime()),
                ToDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodEnd.ToUniversalTime())
            };

            var classroomsResponse = await _classroomClient.GetPagedClassrooms(getClassroomsRequest);

            // Group by course and calculate stats
            var courseStats = new Dictionary<int, CourseStatsAggregate>();

            foreach (var classroom in classroomsResponse.Items)
            {
                var classroomDetail = await _classroomClient.GetClassroomById(
                    new GetClassroomRequest { Id = classroom.Id });

                var courseId = classroomDetail.Course.Id;

                if (!courseStats.ContainsKey(courseId))
                {
                    courseStats[courseId] = new CourseStatsAggregate
                    {
                        CourseId = courseId,
                        CourseCode = classroomDetail.Course.Code,
                        CourseName = classroomDetail.Course.Title,
                        TotalClassrooms = 0,
                        TotalEnrollments = 0,
                        CompletedEnrollments = 0,
                        TotalScore = 0,
                        TotalAttemptCount = 0
                    };
                }

                courseStats[courseId].TotalClassrooms++;

                // Get enrollments for this classroom
                var enrollmentsRequest = new GetCourseEnrollmentsRequest
                {
                    PageNumber = 1,
                    PageSize = 1000,
                    ClassroomId = classroom.Id
                };

                var enrollmentsResponse = await _courseEnrollmentClient.GetPagedCourseEnrollments(enrollmentsRequest);
                courseStats[courseId].TotalEnrollments += enrollmentsResponse.Items.Count;
                courseStats[courseId].CompletedEnrollments += enrollmentsResponse.Items
                    .Count(x => x.Status == "Completed");

                // Get quiz and assignment attempts for all students in this classroom
                var studentIds = classroomDetail.Students.Select(s => s.Id).ToList();
                foreach (var studentId in studentIds)
                {
                    // Get quiz attempts
                    var quizAttemptsRequest = new GetQuizAttemptParams
                    {
                        PageNumber = 1,
                        PageSize = 1000,
                        StudentId = studentId,
                        FromDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodStart.ToUniversalTime()),
                        ToDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodEnd.ToUniversalTime())
                    };

                    var quizAttemptsResponse = await _quizAttemptClient.GetPagedQuizAttempts(quizAttemptsRequest);
                    courseStats[courseId].TotalScore += quizAttemptsResponse.Items.Sum(x => x.TotalScore);
                    courseStats[courseId].TotalAttemptCount += quizAttemptsResponse.Items.Count;

                    // Get assignment attempts
                    var assignmentAttemptsRequest = new GetAssignmentAttemptParams
                    {
                        PageNumber = 1,
                        PageSize = 1000,
                        StudentId = studentId,
                        FromDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodStart.ToUniversalTime()),
                        ToDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodEnd.ToUniversalTime()),
                        ClassroomId = classroom.Id
                    };

                    var assignmentAttemptsResponse = await _assignmentAttemptClient.GetPagedAssignmentAttempts(assignmentAttemptsRequest);

                    courseStats[courseId].TotalScore += assignmentAttemptsResponse.Items.Sum(x => x.TotalScore);
                    courseStats[courseId].TotalAttemptCount += assignmentAttemptsResponse.Items.Count;
                }
            }

            // Convert to TopCourseModel and sort by enrollments
            var topCourses = courseStats.Values
                .Select(stats => new TopCourseModel
                {
                    CourseId = stats.CourseId,
                    CourseCode = stats.CourseCode,
                    CourseName = stats.CourseName,
                    TotalEnrollments = stats.TotalEnrollments,
                    CompletionRate = stats.TotalEnrollments > 0
                        ? (double)stats.CompletedEnrollments / stats.TotalEnrollments * 100
                        : 0,
                    AverageScore = stats.TotalAttemptCount > 0
                        ? stats.TotalScore / stats.TotalAttemptCount
                        : 0,
                    TotalClassrooms = stats.TotalClassrooms
                })
                .OrderByDescending(c => c.TotalEnrollments)
                .Take(10)
                .ToList();

            return topCourses;
        }

        private async Task<List<TopOrganizationModel>> GetTopOrganizations(
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken cancellationToken)
        {
            var startOffset = new DateTimeOffset(periodStart.ToUniversalTime(), TimeSpan.Zero);
            var endOffset = new DateTimeOffset(periodEnd.ToUniversalTime(), TimeSpan.Zero);

            var allOrganizations = await _unitOfWork.Organizations.GetAllAsync(
                cancellationToken: cancellationToken);

            var orgStats = new List<TopOrganizationModel>();

            foreach (var org in allOrganizations)
            {
                var subscriptionOrders = await _unitOfWork.OrganizationSubscriptionOrders
                    .FindAsync(
                        predicate: x => x.OrganizationId == org.Id
                            && x.CreatedDate >= startOffset
                            && x.CreatedDate < endOffset,
                        cancellationToken: cancellationToken);

                var subscriptionOrderIds = subscriptionOrders.Select(x => x.Id).ToList();

                // Get license assignments
                var periodStartUtc = DateTime.SpecifyKind(periodStart, DateTimeKind.Utc);
                var periodEndUtc = DateTime.SpecifyKind(periodEnd, DateTimeKind.Utc);

                var licenseAssignments = await _unitOfWork.LicenseAssignments
                    .FindAsync(
                        predicate: x => subscriptionOrderIds.Contains(x.OrganizationSubscriptionOrderId)
                            && x.AssignedAt >= periodStartUtc
                            && x.AssignedAt < periodEndUtc,
                        cancellationToken: cancellationToken);

                var totalStudents = licenseAssignments
                    .Where(x => x.LicenseType.ToString() == "Student" && x.Status.ToString() == "Active")
                    .Select(x => x.OrganizationUserId)
                    .Distinct()
                    .Count();

                // Get enrollments
                var allEnrollments = new List<GrpcCourseEnrollmentModel>();
                foreach (var subscriptionOrderId in subscriptionOrderIds)
                {
                    var getClassroomsRequest = new GetClassroomsRequest
                    {
                        PageNumber = 1,
                        PageSize = 1000,
                        FromDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodStart.ToUniversalTime()),
                        ToDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodEnd.ToUniversalTime()),
                        OrganizationSubscriptionOrderId = subscriptionOrderId
                    };

                    var classroomsResponse = await _classroomClient.GetPagedClassrooms(getClassroomsRequest);

                    foreach (var classroom in classroomsResponse.Items)
                    {
                        var enrollmentsRequest = new GetCourseEnrollmentsRequest
                        {
                            PageNumber = 1,
                            PageSize = 1000,
                            ClassroomId = classroom.Id
                        };

                        var enrollmentsResponse = await _courseEnrollmentClient.GetPagedCourseEnrollments(enrollmentsRequest);
                        allEnrollments.AddRange(enrollmentsResponse.Items);
                    }
                }

                var completedCount = allEnrollments.Count(x => x.Status == "Completed");
                var passRate = allEnrollments.Count > 0
                    ? (double)completedCount / allEnrollments.Count * 100
                    : 0;

                var activeSubscriptions = subscriptionOrders
                    .Count(x => x.Status.ToString() == "Active");

                orgStats.Add(new TopOrganizationModel
                {
                    OrganizationId = org.Id,
                    OrganizationName = org.Name,
                    OrganizationCode = org.Code,
                    TotalStudents = totalStudents,
                    TotalEnrollments = allEnrollments.Count,
                    PassRate = passRate,
                    ActiveSubscriptions = activeSubscriptions
                });
            }

            return orgStats
                .OrderByDescending(o => o.TotalEnrollments)
                .Take(10)
                .ToList();
        }

        private PeriodComparisonModel CalculatePeriodComparison(
            SystemAdminAggregateData current,
            SystemAdminAggregateData previous)
        {
            return new PeriodComparisonModel
            {
                EnrollmentGrowth = CalculatePercentageChange(current.TotalEnrollments, previous.TotalEnrollments),
                StudentGrowth = CalculatePercentageChange(current.TotalStudents, previous.TotalStudents),
                RevenueGrowth = CalculatePercentageChange((int)current.TotalRevenue, (int)previous.TotalRevenue),
                OrganizationGrowth = CalculatePercentageChange(current.TotalOrganizations, previous.TotalOrganizations)
            };
        }

        private (DateTime, DateTime) GetPeriodDateRange(string period, DateTime now)
        {
            return period.ToLower() switch
            {
                "month" => (
                    new DateTime(now.Year, now.Month, 1),
                    new DateTime(now.Year, now.Month, 1).AddMonths(1)
                ),
                "quarter" => GetQuarterRange(now),
                "year" => (
                    new DateTime(now.Year, 1, 1),
                    new DateTime(now.Year + 1, 1, 1)
                ),
                _ => (
                    new DateTime(now.Year, now.Month, 1),
                    new DateTime(now.Year, now.Month, 1).AddMonths(1)
                )
            };
        }

        private (DateTime, DateTime) GetQuarterRange(DateTime now)
        {
            int quarter = (now.Month - 1) / 3 + 1;
            int startMonth = (quarter - 1) * 3 + 1;
            var quarterStart = new DateTime(now.Year, startMonth, 1);
            var quarterEnd = quarterStart.AddMonths(3);
            return (quarterStart, quarterEnd);
        }

        private (DateTime, DateTime) GetPreviousPeriodDateRange(string period, DateTime currentStart)
        {
            return period.ToLower() switch
            {
                "month" => (currentStart.AddMonths(-1), currentStart),
                "quarter" => (currentStart.AddMonths(-3), currentStart),
                "year" => (currentStart.AddYears(-1), currentStart),
                _ => (currentStart.AddMonths(-1), currentStart)
            };
        }

        private double CalculatePercentageChange(int current, int previous)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;

            return Math.Round(((double)(current - previous) / previous) * 100, 1);
        }
    }
}

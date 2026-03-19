using MediatR;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Cache;
using Order.Application.Common.Interfaces.Grpc;
using Order.Application.Specifications;
using Shared.Protos.Classroom;
using Shared.Protos.Order;

namespace Order.Application.Queries.Organizations.GetOrganizationDashboard
{
    public class GetOrganizationDashboardQueryHandler
        : IRequestHandler<GetOrganizationDashboardQuery, GetOrganizationDashboardResponse>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IGrpcClassroomClient _classroomClient;
        private readonly IGrpcCurriculumEnrollmentClient _curriculumEnrollmentClient;
        private readonly IGrpcCourseEnrollmentClient _courseEnrollmentClient;
        private readonly IGrpcCertificateClient _certificateClient;
        private readonly IGrpcCurriculumClient _curriculumClient;
        private readonly IGrpcQuizAttemptClient _grpcQuizAttemptClient;
        private readonly IGrpcAssignmentAttemptClient _assignmentAttemptClient;

        public GetOrganizationDashboardQueryHandler(
            IOrderUnitOfWork unitOfWork,
            IGrpcCurriculumEnrollmentClient curriculumEnrollmentClient,
            IGrpcCertificateClient certificateClient,
            IGrpcCurriculumClient curriculumClient,
            IGrpcQuizAttemptClient grpcQuizAttemptClient,
            IGrpcCourseEnrollmentClient courseEnrollmentClient,
            IGrpcAssignmentAttemptClient assignmentAttemptClient,
            IGrpcClassroomClient classroomClient)
        {
            _unitOfWork = unitOfWork;
            _classroomClient = classroomClient;
            _curriculumEnrollmentClient = curriculumEnrollmentClient;
            _certificateClient = certificateClient;
            _curriculumClient = curriculumClient;
            _grpcQuizAttemptClient = grpcQuizAttemptClient;
            _courseEnrollmentClient = courseEnrollmentClient;
            _assignmentAttemptClient = assignmentAttemptClient;
        }

        public async Task<GetOrganizationDashboardResponse> Handle(
            GetOrganizationDashboardQuery request,
            CancellationToken cancellationToken
        )
        {
            var organizationId = request.Id;

            var organization = await _unitOfWork.Organizations.FindByIdAsync(
                organizationId,
                cancellationToken
            );
            if (organization == null)
            {
                throw new KeyNotFoundException($"Organization with ID {organizationId} not found.");
            }

            var period = request.Period ?? "Month";

            var (currentPeriodStart, currentPeriodEnd) = GetPeriodDateRange(period, DateTime.UtcNow);
            var (previousPeriodStart, previousPeriodEnd) = GetPreviousPeriodDateRange(period, currentPeriodStart);

            // Fetch current period data
            var currentData = await GetPeriodStatistics(
                organizationId,
                currentPeriodStart,
                currentPeriodEnd,
                cancellationToken
            );

            // Fetch previous period data for comparison
            var previousData = await GetPeriodStatistics(
                organizationId,
                previousPeriodStart,
                previousPeriodEnd,
                cancellationToken
            );

            // Calculate changes
            var changes = CalculateChanges(currentData, previousData);

            // Fetch detailed statistics
            var curriculumStats = await GetCurriculumStatistics(
                organizationId,
                currentPeriodStart,
                currentPeriodEnd,
                cancellationToken
            );

            var classroomStats = await GetClassroomStatistics(
                organizationId,
                currentPeriodStart,
                currentPeriodEnd,
                cancellationToken
            );

            var response = new GetOrganizationDashboardResponse
            {
                CurrentPeriod = new PeriodStatisticModel
                {
                    TotalCurriculum = currentData.TotalCurriculum,
                    TotalClassrooms = currentData.TotalClassrooms,
                    TotalStudents = currentData.TotalStudents,
                    TotalTeachers = currentData.TotalTeachers,
                    TotalUsers = currentData.TotalUsers,
                    TotalCurriculumEnrollments = currentData.TotalCurriculumEnrollments,
                    TotalCurriculumCertificates = currentData.TotalCurriculumCertificates,
                    PassRate = currentData.PassRate
                },
                PreviousPeriod = new PeriodStatisticModel
                {
                    TotalCurriculum = previousData.TotalCurriculum,
                    TotalClassrooms = previousData.TotalClassrooms,
                    TotalStudents = previousData.TotalStudents,
                    TotalTeachers = previousData.TotalTeachers,
                    TotalUsers = previousData.TotalUsers,
                    TotalCurriculumEnrollments = previousData.TotalCurriculumEnrollments,
                    TotalCurriculumCertificates = previousData.TotalCurriculumCertificates,
                    PassRate = previousData.PassRate
                },
                Change = changes
            };

            if (curriculumStats != null && curriculumStats.Any())
            {
                response.CurriculumStatistics.AddRange(
                    curriculumStats.Select(c => new CurriculumStatisticModel
                    {
                        Id = c.Id,
                        Title = c.Title ?? string.Empty,
                        ImageUrl = c.ImageUrl ?? string.Empty,
                        CourseCount = c.CourseCount,
                        PassRate = c.PassRate,
                        TotalEnrollment = c.TotalEnrollment
                    })
                );
            }

            if (classroomStats != null && classroomStats.Any())
            {
                response.ClassroomStatistics.AddRange(
                    classroomStats.Select(c => new ClassroomStatisticModel
                    {
                        Id = c.Id,
                        Name = c.Name ?? string.Empty,
                        PassRate = c.PassRate,
                        AverageScore = c.AverageScore,
                        CourseTitle = c.CourseTitle,
                        CourseCode = c.CourseCode
                    })
                );
            }

            return response;
        }

        private async Task<PeriodStatisticModel> GetPeriodStatistics(
            int organizationId,
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken cancellationToken
        )
        {
            // Step 1: Get subscription orders from Order service
            var startOffset = new DateTimeOffset(periodStart.ToUniversalTime(), TimeSpan.Zero);
            var endOffset = new DateTimeOffset(periodEnd.ToUniversalTime(), TimeSpan.Zero);

            var spec = new OrganizationSubscriptionOrdersWithCurriculumsSpec(
                organizationId,
                startOffset,
                endOffset
            );

            var subscriptionOrders = await _unitOfWork.OrganizationSubscriptionOrders
                .GetAllAsync(spec, cancellationToken);

            var subscriptionOrderIds = subscriptionOrders.Select(x => x.Id).ToList();

            // Step 2: Get license assignments from Order service
            var periodStartUtc = DateTime.SpecifyKind(periodStart, DateTimeKind.Utc);
            var periodEndUtc = DateTime.SpecifyKind(periodEnd, DateTimeKind.Utc);

            var licenseAssignments = await _unitOfWork.LicenseAssignments
                .FindAsync(
                    predicate: x => subscriptionOrderIds.Contains(x.OrganizationSubscriptionOrderId)
                        && x.AssignedAt >= periodStartUtc
                        && x.AssignedAt < periodEndUtc,
                    cancellationToken: cancellationToken
                );

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

            var totalUsers = licenseAssignments
                //.Where(x => x.Status.ToString() == "Active")
                .Select(x => x.OrganizationUserId)
                .Distinct()
                .Count();

            // Step 3: Get classrooms from Classroom service using subscription order IDs
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

                // Step 4: For each classroom, get curriculum enrollments and related data
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

                    // Step 5: Get certificates for these enrollments
                    var enrollmentIds = enrollmentsResponse.Items.Select(x => x.Id).ToList();
                    foreach (var enrollmentId in enrollmentIds)
                    {
                        var certificatesRequest = new GetCertificatesRequest
                        {
                            PageNumber = 1,
                            PageSize = 1000,
                            CourseEnrollmentId = enrollmentId
                        };

                        var certificatesResponse = await _certificateClient.GetPagedCertificates(certificatesRequest);
                        allCertificates.AddRange(certificatesResponse.Items);
                    }
                }
            }

            // Step 6: Calculate statistics
            var totalCurriculum = subscriptionOrders
                .SelectMany(o => o.SubscriptionOrderCurriculums?.Select(s => s.CurriculumId) ?? Enumerable.Empty<int>())
                .Distinct()
                .Count();

            var totalClassrooms = allClassrooms.Count();
            var totalCurriculumEnrollments = allCourseEnrollments.Count();
            var totalCertificates = allCertificates.Count();

            var completedEnrollments = allCourseEnrollments
                .Where(x => x.Status == "Completed")
                .Count();

            var passRate = allCourseEnrollments.Count() > 0
                ? (double)completedEnrollments / allCourseEnrollments.Count() * 100
                : 0;

            return new PeriodStatisticModel
            {
                TotalCurriculum = totalCurriculum,
                TotalClassrooms = totalClassrooms,
                TotalStudents = totalStudents,
                TotalTeachers = totalTeachers,
                TotalUsers = totalUsers,
                TotalCurriculumEnrollments = totalCurriculumEnrollments,
                TotalCurriculumCertificates = totalCertificates,
                PassRate = passRate
            };
        }

        private async Task<List<CurriculumStatisticModel>> GetCurriculumStatistics(
            int organizationId,
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken cancellationToken
        )
        {
            var spec = new OrganizationSubscriptionOrderByOrganizationIdSpecification(organizationId);

            var subscriptionOrders = await _unitOfWork.OrganizationSubscriptionOrders
                .GetAllAsync(
                    specification: spec,
                    cancellationToken: cancellationToken
                );

            var stats = new List<CurriculumStatisticModel>();

            var distinctCurriculumIds = subscriptionOrders
                .SelectMany(o => o.SubscriptionOrderCurriculums?.Select(sc => sc.CurriculumId) ?? Enumerable.Empty<int>())
                .Distinct()
                .ToList();

            foreach (var curriculumId in distinctCurriculumIds)
            {
                var curriculum = await _curriculumClient.GetCurriculumByIdAsync(
                    curriculumId
                );

                var allEnrollmentsForCurriculum = new List<GrpcCourseEnrollmentModel>();

                foreach (var course in curriculum.Courses)
                {
                    var getClassroomsRequest = new GetClassroomsRequest
                    {
                        PageNumber = 1,
                        PageSize = 1000,
                        FromDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodStart.ToUniversalTime()),
                        ToDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodEnd.ToUniversalTime()),
                        CourseId = course.Id
                    };

                    var classroomsResponse = await _classroomClient.GetPagedClassrooms(getClassroomsRequest);

                    foreach (var classroom in classroomsResponse.Items)
                    {
                        var enrollmentsRequest = new GetCourseEnrollmentsRequest
                        {
                            PageNumber = 1,
                            PageSize = 1000,
                            ClassroomId = classroom.Id,
                        };

                        var enrollmentsResponse = await _courseEnrollmentClient.GetPagedCourseEnrollments(enrollmentsRequest);
                        allEnrollmentsForCurriculum.AddRange(enrollmentsResponse.Items);
                    }
                }

                var completedCount = allEnrollmentsForCurriculum
                    .Where(x => x.Status == "Completed")
                    .Count();

                var passRate = allEnrollmentsForCurriculum.Count() > 0
                    ? (double)completedCount / allEnrollmentsForCurriculum.Count() * 100
                    : 0;

                stats.Add(new CurriculumStatisticModel
                {
                    Id = curriculumId,
                    Title = curriculum?.Title ?? "Unknown",
                    ImageUrl = curriculum?.ImageUrl ?? string.Empty,
                    CourseCount = curriculum?.CourseCount ?? 0,
                    PassRate = passRate,
                    TotalEnrollment = allEnrollmentsForCurriculum.Count()
                });
            }

            return stats;
        }

        private async Task<List<ClassroomStatisticModel>> GetClassroomStatistics(
            int organizationId,
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken cancellationToken
        )
        {
            var startOffset = new DateTimeOffset(periodStart.ToUniversalTime(), TimeSpan.Zero);
            var endOffset = new DateTimeOffset(periodEnd.ToUniversalTime(), TimeSpan.Zero);

            var subscriptionOrders = await _unitOfWork.OrganizationSubscriptionOrders
                .FindAsync(
                    predicate: x => x.OrganizationId == organizationId
                        && x.CreatedDate >= startOffset
                        && x.CreatedDate < endOffset,
                    cancellationToken: cancellationToken
                );

            var stats = new List<ClassroomStatisticModel>();
            var subscriptionOrderIds = subscriptionOrders.Select(x => x.Id).ToList();

            foreach (var subscriptionOrderId in subscriptionOrderIds)
            {
                // Get classrooms for this subscription order
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
                    // Get classroom details with students
                    var getClassroomRequest = new GetClassroomRequest { Id = classroom.Id };
                    var classroomDetail = await _classroomClient.GetClassroomById(getClassroomRequest);

                    var studentIds = classroomDetail.Students.Select(s => s.Id).ToList();

                    var averageScore = 0.0;
                    var passRate = 0.0;
                    var allQuizAttempts = new List<GrpcQuizAttemptModel>();
                    var allAssignmentAttempts = new List<GrpcAssignmentAttemptModel>();

                    // Get quiz and assignment attempts for all students in this classroom
                    if (studentIds.Any())
                    {
                        foreach (var studentId in studentIds)
                        {
                            var quizAttemptsRequest = new GetQuizAttemptParams
                            {
                                PageNumber = 1,
                                PageSize = 1000,
                                StudentId = studentId,
                                FromDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodStart.ToUniversalTime()),
                                ToDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(periodEnd.ToUniversalTime())
                            };
                            var quizAttemptsResponse = await _grpcQuizAttemptClient.GetPagedQuizAttempts(quizAttemptsRequest);
                            allQuizAttempts.AddRange(quizAttemptsResponse.Items);

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

                            allAssignmentAttempts.AddRange(assignmentAttemptsResponse.Items);
                        }

                        var totalAttempts = allQuizAttempts.Count + allAssignmentAttempts.Count;

                        if (totalAttempts > 0)
                        {
                            var totalScore = allQuizAttempts.Sum(x => x.TotalScore) +
                                           allAssignmentAttempts.Sum(x => x.TotalScore);
                            averageScore = totalScore / totalAttempts;

                            int passedAttempts = 0;

                            foreach (var attempt in allQuizAttempts)
                            {
                                if (attempt.Quiz != null && attempt.TotalScore >= attempt.Quiz.PassingMarks)
                                {
                                    passedAttempts++;
                                }
                            }

                            foreach (var attempt in allAssignmentAttempts)
                            {
                                if (attempt.Assignment != null && attempt.TotalScore >= attempt.Assignment.PassingScore)
                                {
                                    passedAttempts++;
                                }
                            }

                            passRate = (double)passedAttempts / totalAttempts * 100;
                        }
                    }

                    stats.Add(new ClassroomStatisticModel
                    {
                        Id = classroom.Id,
                        Name = classroom.Name,
                        PassRate = passRate,
                        AverageScore = averageScore,
                        CourseCode = classroomDetail.Course.Code,
                        CourseTitle = classroomDetail.Course.Title
                    });
                }
            }

            return stats;
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
                "month" => (
                    currentStart.AddMonths(-1),
                    currentStart
                ),
                "quarter" => (
                    currentStart.AddMonths(-3),
                    currentStart
                ),
                "year" => (
                    currentStart.AddYears(-1),
                    currentStart
                ),
                _ => (
                    currentStart.AddMonths(-1),
                    currentStart
                )
            };
        }

        private OrganizationChangeModel CalculateChanges(PeriodStatisticModel current, PeriodStatisticModel previous)
        {
            return new OrganizationChangeModel
            {
                TotalCurriculum = CalculatePercentageChange(current.TotalCurriculum, previous.TotalCurriculum),
                TotalClassrooms = CalculatePercentageChange(current.TotalClassrooms, previous.TotalClassrooms),
                TotalStudents = CalculatePercentageChange(current.TotalStudents, previous.TotalStudents),
                TotalTeachers = CalculatePercentageChange(current.TotalTeachers, previous.TotalTeachers),
                TotalUsers = CalculatePercentageChange(current.TotalUsers, previous.TotalUsers),
                TotalCurriculumEnrollments = CalculatePercentageChange(current.TotalCurriculumEnrollments, previous.TotalCurriculumEnrollments),
                TotalCurriculumCertificates = CalculatePercentageChange(current.TotalCurriculumCertificates, previous.TotalCurriculumCertificates),
                PassRate = Math.Round(current.PassRate - previous.PassRate, 1)
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
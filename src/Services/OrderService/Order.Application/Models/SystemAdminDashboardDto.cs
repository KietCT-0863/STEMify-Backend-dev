using Shared.Protos.Order;

namespace Order.Application.Models
{
    public class SystemAdminAggregateData
    {
        public int TotalEnrollments { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalClassrooms { get; set; }
        public int TotalCertificates { get; set; }
        public int TotalOrganizations { get; set; }
        public double OverallPassRate { get; set; }
        public SystemAdminSubscriptionStats SubscriptionStats { get; set; }
        public SystemAdminEnrollmentStats EnrollmentStats { get; set; }
        public double TotalRevenue { get; set; }
    }

    public class CourseStatsAggregate
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int TotalClassrooms { get; set; }
        public int TotalEnrollments { get; set; }
        public int CompletedEnrollments { get; set; }
        public double TotalScore { get; set; }
        public int TotalAttemptCount { get; set; }
    }
}

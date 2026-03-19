namespace Order.Application.Models
{
    public class PeriodStatistics
    {
        public int TotalCurriculum { get; set; }
        public int TotalClassrooms { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalUsers { get; set; }
        public int TotalCurriculumEnrollments { get; set; }
        public int TotalCurriculumCertificates { get; set; }
        public double PassRate { get; set; }
    }
}

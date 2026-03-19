using System;

namespace EventBus.Messages
{
    public class ClassroomStudentProgressUpdatedEvent : IntegrationEvent
    {
        public string StudentId { get; set; } = string.Empty;

        public int? ClassroomId { get; set; }

        public int CourseEnrollmentId { get; set; }

        public int CourseId { get; set; }
        public int ProgressPercentage { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}



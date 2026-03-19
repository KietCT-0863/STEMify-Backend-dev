namespace Classroom.Domain.Enums
{
    public enum StudentQuizStatus
    {
        Assigned, // The quiz has been assigned but not yet started
        InProgress, // The quiz is currently being attempted
        Passed, // The quiz has been completed and passed
        Failed, // The quiz has been completed and failed
        Expired, // The quiz has expired
        Locked // student is over the max attempts allowed
    }
}

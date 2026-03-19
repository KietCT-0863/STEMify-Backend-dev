namespace Resource.Domain.Enums
{
    public enum CourseStatus
    {
        Draft, // Course is being developed, not visible to learners.
        Published, // Live and available for learners.
        Archived, // Archived, no longer actively maintained but still accessible.
        Deleted, // Course is deleted, no longer available.
    }
}

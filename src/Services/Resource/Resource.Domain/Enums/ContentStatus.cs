namespace Resource.Domain.Enums
{
    public enum ContentStatus
    {
        Draft, // Still being created
        Published, // Live and available for learners.
        Archived, // Old or superseded content.
        Deleted, // Permanently removed from the system, no record kept.
    }
}

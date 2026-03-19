namespace Shared.Enums
{
    public enum ContentStatus
    {
        Draft = 0, // Still being created
        Published = 1, // Live and available for learners.
        Archived = 2, // Old or superseded content.
        Deleted = 3, // Permanently removed from the system, no record kept.
    }
}

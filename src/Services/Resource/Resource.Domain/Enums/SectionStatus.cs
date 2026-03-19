namespace Resource.Domain.Enums
{
    public enum SectionStatus
    {
        Draft, // Not finalized.
        Published, // Live and available for learners.
        Archived, // No longer available to new learners, but record kept.
        Deleted, // Permanently removed from the system, no record kept.
    }
}

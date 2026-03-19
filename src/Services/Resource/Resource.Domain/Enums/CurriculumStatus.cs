namespace Resource.Domain.Enums
{
    public enum CurriculumStatus
    {
        Draft = 1, // Curriculum is being developed, not visible to learners.
        Published = 2, // Live and available for learners.
        Archived = 3, // Archived, no longer actively maintained but still accessible.
        Deleted = 4, // Curriculum is deleted, no longer available.
    }
}

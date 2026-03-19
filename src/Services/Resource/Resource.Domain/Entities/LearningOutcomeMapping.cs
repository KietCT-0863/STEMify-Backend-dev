namespace Resource.Domain.Entities
{
    public class LearningOutcomeMapping
    {
        public int CLOId { get; set; }
        public int PLOId { get; set; }

        public virtual CourseLearningOutcome CourseLearningOutcome { get; set; }
        public virtual ProgramLearningOutcome ProgramLearningOutcome { get; set; }
    }
}

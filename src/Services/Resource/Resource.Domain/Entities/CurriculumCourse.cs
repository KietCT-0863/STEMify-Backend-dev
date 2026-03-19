using Contracts.Domains;

namespace Resource.Domain.Entities
{
    public class CurriculumCourse : EntityBase<int>
    {
        public int CourseId { get; set; }

        public int CurriculumId { get; set; }
        public int CourseOrderIndex { get; set; }

        public virtual Course Course { get; set; }
        public virtual Curriculum Curriculum { get; set; }
    }
}

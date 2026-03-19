namespace Resource.Domain.Entities
{
    public class LessonSkill
    {
        public int LessonId { get; set; }
        public int SkillId { get; set; }

        public virtual Lesson Lesson { get; set; }
        public virtual Skill Skill { get; set; }
    }
}

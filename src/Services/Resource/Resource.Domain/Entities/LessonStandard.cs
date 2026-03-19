namespace Resource.Domain.Entities
{
    public class LessonStandard
    {
        public int LessonId { get; set; }
        public int StandardId { get; set; }

        public virtual Lesson Lesson { get; set; }
        public virtual Standard Standard { get; set; }
    }
}

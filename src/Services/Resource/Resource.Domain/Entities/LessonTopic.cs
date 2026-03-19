using System.ComponentModel.DataAnnotations.Schema;

namespace Resource.Domain.Entities
{
    public class LessonTopic
    {
        [ForeignKey("Lesson")]
        public int LessonId { get; set; }

        [ForeignKey("Topic")]
        public int TopicId { get; set; }

        // Navigation properties

        public virtual Lesson Lesson { get; set; }
        public virtual Topic Topic { get; set; }
    }
}

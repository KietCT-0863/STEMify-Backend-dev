using Contracts.Domains;
using Resource.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resource.Domain.Entities
{
    public class Question : EntityBase<int>
    {
        public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;

        [Required]
        public int OrderIndex { get; set; }

        [ForeignKey("Quiz")]
        public int QuizId { get; set; }
        [Required]
        public string Content { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public string? AnswerExplanation { get; set; }
        public int Points { get; set; }

        // Navigation properties
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public virtual Quiz Quiz { get; set; } = null!;
    }
}

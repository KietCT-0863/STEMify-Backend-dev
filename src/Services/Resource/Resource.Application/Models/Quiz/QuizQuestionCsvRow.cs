using System.ComponentModel.DataAnnotations;

namespace Resource.Application.Models.Quiz
{
    public class QuizQuestionCsvRow
    {
        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Points is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Points must be a positive number")]
        public int Points { get; set; }

        public string? AnswerExplanation { get; set; }

        [Required(ErrorMessage = "At least Option A is required")]
        public string OptionA { get; set; } = string.Empty;

        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string? OptionE { get; set; }
        public string? OptionF { get; set; }

        [Required(ErrorMessage = "Correct Answer is required")]
        [RegularExpression("^[A-F]$", ErrorMessage = "Correct Answer must be A, B, C, D, E, or F")]
        public string CorrectAnswer { get; set; } = string.Empty;

        // Row number for error reporting
        public int RowNumber { get; set; }
    }
}

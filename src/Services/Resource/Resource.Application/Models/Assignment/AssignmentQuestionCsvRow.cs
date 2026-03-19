using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Resource.Application.Models.Quiz
{
    public class AssignmentQuestionCsvRow
    {
        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Points is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Points must be a positive number")]
        public decimal Points { get; set; }

        public string? AnswerExplanation { get; set; }

        // Criteria with their max points
        public string? CriterionA { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "CriterionA MaxPoints must be positive")]
        public decimal? CriterionAMaxPoints { get; set; }

        public string? CriterionB { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "CriterionB MaxPoints must be positive")]
        public decimal? CriterionBMaxPoints { get; set; }

        public string? CriterionC { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "CriterionC MaxPoints must be positive")]
        public decimal? CriterionCMaxPoints { get; set; }

        public string? CriterionD { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "CriterionD MaxPoints must be positive")]
        public decimal? CriterionDMaxPoints { get; set; }

        public string? CriterionE { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "CriterionE MaxPoints must be positive")]
        public decimal? CriterionEMaxPoints { get; set; }

        public string? CriterionF { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "CriterionF MaxPoints must be positive")]
        public decimal? CriterionFMaxPoints { get; set; }

        // Row number for error reporting
        public int RowNumber { get; set; }
    }
}

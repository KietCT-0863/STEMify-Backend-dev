using Resource.Application.Models.Quiz;
using Shared.Protos.Resource;

namespace Resource.Application.Common.Interfaces
{
    public interface ICsvParserService
    {
        Task<(List<QuizQuestionCsvRow> rows, List<QuizImportError> errors)> ParseQuizQuestionsCsvAsync(Stream csvStream);
        Task<(List<AssignmentQuestionCsvRow> rows, List<AssignmentImportError> errors)> ParseAssignmentQuestionsCsvAsync(Stream csvStream);
    }
}

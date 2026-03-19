using MediatR;
using Microsoft.Extensions.Logging;
using Resource.Application.Commands.Quiz;
using Resource.Application.Common.Interfaces;
using Resource.Application.Models.Quiz;
using Resource.Application.Specifications.Questions;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Quiz
{
    public class ImportQuizQuestionsCommandHandler : IRequestHandler<ImportQuizQuestionsCommand, QuizImportResult>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly ICsvParserService _csvParserService;
        private readonly ILogger<ImportQuizQuestionsCommandHandler> _logger;

        public ImportQuizQuestionsCommandHandler(
            IResourceUnitOfWork unitOfWork,
            ICsvParserService csvParserService,
            ILogger<ImportQuizQuestionsCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _csvParserService = csvParserService;
            _logger = logger;
        }

        public async Task<QuizImportResult> Handle(
            ImportQuizQuestionsCommand request,
            CancellationToken cancellationToken)
        {
            var result = new QuizImportResult();

            try
            {
                // Validate file bytes
                if (request.CsvFileBytes == null || request.CsvFileBytes.Length == 0)
                {
                    result.Errors.Add(new QuizImportError
                    {
                        RowNumber = 0,
                        Field = "File",
                        ErrorMessage = "CSV file is required",
                        RowData = string.Empty
                    });
                    return result;
                }

                // Verify quiz exists
                var quiz = await _unitOfWork.Quizzes.FindByIdAsync(request.QuizId, cancellationToken);
                if (quiz == null)
                {
                    result.Errors.Add(new QuizImportError
                    {
                        RowNumber = 0,
                        Field = "QuizId",
                        ErrorMessage = $"Quiz with ID {request.QuizId} not found",
                        RowData = string.Empty
                    });
                    return result;
                }

                // Get existing questions to append after them (preserve order)
                var spec = new QuestionByQuizIdSpecification(request.QuizId);
                var existingQuestions = await _unitOfWork.Questions.GetAllAsync(spec, cancellationToken);
                var startIndex = (existingQuestions != null && existingQuestions.Any())
                    ? existingQuestions.Max(q => q.OrderIndex) + 1
                    : 1;

                // Parse CSV from bytes
                using var stream = new MemoryStream(request.CsvFileBytes);
                var (rows, parseErrors) = await _csvParserService.ParseQuizQuestionsCsvAsync(stream);

                result.TotalRows = rows.Count;
                result.Errors.AddRange(parseErrors);

                if (!rows.Any())
                {
                    if (!result.Errors.Any())
                    {
                        result.Errors.Add(new QuizImportError
                        {
                            RowNumber = 0,
                            Field = "File",
                            ErrorMessage = "CSV file contains no valid data",
                            RowData = string.Empty
                        });
                    }
                    return result;
                }

                // Convert CSV rows to Question entities
                var questions = new List<Resource.Domain.Entities.Question>();
                var orderIndex = startIndex;

                foreach (var row in rows)
                {
                    try
                    {
                        var question = ConvertToQuestionEntity(row, request.QuizId, orderIndex);
                        questions.Add(question);
                        orderIndex++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error converting row {RowNumber} to question", row.RowNumber);
                        result.Errors.Add(new QuizImportError
                        {
                            RowNumber = row.RowNumber,
                            Field = "Row",
                            ErrorMessage = $"Error processing row: {ex.Message}",
                            RowData = $"Row {row.RowNumber}"
                        });
                        result.FailureCount++;
                    }
                }

                // Save questions to database (extend existing)
                if (questions.Any())
                {
                    try
                    {
                        await _unitOfWork.Questions.AddRangeAsync(questions, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);

                        result.SuccessCount = questions.Count;

                        _logger.LogInformation(
                            "Successfully imported {SuccessCount} questions for quiz {QuizId}",
                            result.SuccessCount,
                            request.QuizId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving questions to database");
                        result.Errors.Add(new QuizImportError
                        {
                            RowNumber = 0,
                            Field = "Database",
                            ErrorMessage = $"Error saving questions: {ex.Message}",
                            RowData = string.Empty
                        });
                        result.FailureCount = questions.Count;
                        result.SuccessCount = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during quiz import");
                result.Errors.Add(new QuizImportError
                {
                    RowNumber = 0,
                    Field = "System",
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                    RowData = string.Empty
                });
            }

            return result;
        }

        private Resource.Domain.Entities.Question ConvertToQuestionEntity(
            QuizQuestionCsvRow row,
            int quizId,
            int orderIndex)
        {
            var question = new Resource.Domain.Entities.Question
            {
                QuizId = quizId,
                QuestionType = Resource.Domain.Enums.QuestionType.MultipleChoice,
                Content = row.Content,
                OrderIndex = orderIndex,
                Points = row.Points,
                AnswerExplanation = row.AnswerExplanation,
                Answers = new List<Resource.Domain.Entities.Answer>()
            };

            // Add all non-empty options as answers
            var options = new[]
            {
                ("A", row.OptionA),
                ("B", row.OptionB),
                ("C", row.OptionC),
                ("D", row.OptionD),
                ("E", row.OptionE),
                ("F", row.OptionF)
            };

            foreach (var (letter, content) in options)
            {
                if (!string.IsNullOrWhiteSpace(content))
                {
                    question.Answers.Add(new Resource.Domain.Entities.Answer
                    {
                        Content = content,
                        IsCorrect = letter == row.CorrectAnswer
                    });
                }
            }

            return question;
        }
    }
}
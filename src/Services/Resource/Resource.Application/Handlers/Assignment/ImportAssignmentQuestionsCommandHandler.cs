using MediatR;
using Microsoft.Extensions.Logging;
using Resource.Application.Commands.Assignment;
using Resource.Application.Common.Interfaces;
using Resource.Application.Models.Quiz;
using Resource.Application.Specifications.Assignments;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Assignment
{
    public class ImportAssignmentQuestionsCommandHandler : IRequestHandler<ImportAssignmentQuestionsCommand, AssignmentImportResult>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly ICsvParserService _csvParserService;
        private readonly ILogger<ImportAssignmentQuestionsCommandHandler> _logger;

        public ImportAssignmentQuestionsCommandHandler(
            IResourceUnitOfWork unitOfWork,
            ICsvParserService csvParserService,
            ILogger<ImportAssignmentQuestionsCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _csvParserService = csvParserService;
            _logger = logger;
        }

        public async Task<AssignmentImportResult> Handle(
            ImportAssignmentQuestionsCommand request,
            CancellationToken cancellationToken)
        {
            var result = new AssignmentImportResult();

            try
            {
                // Validate file bytes
                if (request.CsvFileBytes == null || request.CsvFileBytes.Length == 0)
                {
                    result.Errors.Add(new AssignmentImportError
                    {
                        RowNumber = 0,
                        Field = "File",
                        ErrorMessage = "CSV file is required",
                        RowData = string.Empty
                    });
                    return result;
                }

                var assignment = await _unitOfWork.Assignments.FindByIdAsync(request.AssignmentId, cancellationToken);
                if (assignment == null)
                {
                    result.Errors.Add(new AssignmentImportError
                    {
                        RowNumber = 0,
                        Field = "AssignmentId",
                        ErrorMessage = $"Assignment with ID {request.AssignmentId} not found",
                        RowData = string.Empty
                    });
                    return result;
                }

                var spec = new QuestionByAssignmentIdSpecification(request.AssignmentId);
                var existingQuestions = await _unitOfWork.AssignmentQuestions.GetAllAsync(spec, cancellationToken);
                var startIndex = (existingQuestions != null && existingQuestions.Any())
                    ? existingQuestions.Max(q => q.OrderIndex) + 1
                    : 1;

                // Parse CSV from bytes
                using var stream = new MemoryStream(request.CsvFileBytes);
                var (rows, parseErrors) = await _csvParserService.ParseAssignmentQuestionsCsvAsync(stream);

                result.TotalRows = rows.Count;
                result.Errors.AddRange(parseErrors);

                if (!rows.Any())
                {
                    if (!result.Errors.Any())
                    {
                        result.Errors.Add(new AssignmentImportError
                        {
                            RowNumber = 0,
                            Field = "File",
                            ErrorMessage = "CSV file contains no valid data",
                            RowData = string.Empty
                        });
                    }
                    return result;
                }

                // Convert CSV rows to AssignmentQuestion entities
                var questions = new List<Resource.Domain.Entities.AssignmentQuestion>();
                var orderIndex = startIndex;

                foreach (var row in rows)
                {
                    try
                    {
                        var question = ConvertToQuestionEntity(row, request.AssignmentId, orderIndex);
                        questions.Add(question);
                        orderIndex++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error converting row {RowNumber} to question", row.RowNumber);
                        result.Errors.Add(new AssignmentImportError
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
                        await _unitOfWork.AssignmentQuestions.AddRangeAsync(questions, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);

                        result.SuccessCount = questions.Count;

                        _logger.LogInformation(
                            "Successfully imported {SuccessCount} assignment questions for assignment {AssignmentId}",
                            result.SuccessCount,
                            request.AssignmentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving questions to database");
                        result.Errors.Add(new AssignmentImportError
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
                _logger.LogError(ex, "Unexpected error during assignment import");
                result.Errors.Add(new AssignmentImportError
                {
                    RowNumber = 0,
                    Field = "System",
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                    RowData = string.Empty
                });
            }

            return result;
        }

        private Resource.Domain.Entities.AssignmentQuestion ConvertToQuestionEntity(
            AssignmentQuestionCsvRow row,
            int assignmentId,
            int orderIndex)
        {
            var question = new Resource.Domain.Entities.AssignmentQuestion
            {
                AssignmentId = assignmentId,
                Content = row.Content,
                OrderIndex = orderIndex,
                Points = row.Points,
                Type = Domain.Enums.AssignmentQuestionType.Text,
                RubricCriterions = new List<Resource.Domain.Entities.RubricCriterion>()
            };

            // Map rubric criteria (A-F) -> RubricCriterion entities
            var criteria = new (string? Name, decimal? MaxPoints)[]
            {
                (row.CriterionA, row.CriterionAMaxPoints),
                (row.CriterionB, row.CriterionBMaxPoints),
                (row.CriterionC, row.CriterionCMaxPoints),
                (row.CriterionD, row.CriterionDMaxPoints),
                (row.CriterionE, row.CriterionEMaxPoints),
                (row.CriterionF, row.CriterionFMaxPoints)
            };

            foreach (var (name, maxPoints) in criteria)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    question.RubricCriterions.Add(new Resource.Domain.Entities.RubricCriterion
                    {
                        CriterionName = name,
                        MaxPoints = maxPoints ?? 100m
                    });
                }
            }

            return question;
        }
    }
}
using CsvHelper;
using CsvHelper.Configuration;
using Resource.Application.Common.Interfaces;
using Resource.Application.Models.Quiz;
using Shared.Protos.Resource;
using System.Globalization;
using System.Text;

namespace Resource.Infrastructure.Services
{
    public class CsvParserService : ICsvParserService
    {
        public async Task<(List<QuizQuestionCsvRow> rows, List<QuizImportError> errors)> ParseQuizQuestionsCsvAsync(Stream csvStream)
        {
            var rows = new List<QuizQuestionCsvRow>();
            var errors = new List<QuizImportError>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = context =>
                {
                    errors.Add(new QuizImportError
                    {
                        RowNumber = context.Context.Parser.Row,
                        Field = "Row",
                        ErrorMessage = $"Bad data found: {context.RawRecord}",
                        RowData = context.RawRecord
                    });
                }
            };

            try
            {
                using var reader = new StreamReader(csvStream, Encoding.UTF8);
                using var csv = new CsvReader(reader, config);

                await csv.ReadAsync();
                csv.ReadHeader();

                int rowNumber = 1;
                while (await csv.ReadAsync())
                {
                    rowNumber++;
                    try
                    {
                        var row = new QuizQuestionCsvRow
                        {
                            RowNumber = rowNumber,
                            Content = csv.GetField<string>("Content")?.Trim() ?? string.Empty,
                            Points = csv.GetField<int>("Points"),
                            AnswerExplanation = csv.GetField<string>("AnswerExplanation")?.Trim(),
                            OptionA = csv.GetField<string>("OptionA")?.Trim() ?? string.Empty,
                            OptionB = csv.GetField<string>("OptionB")?.Trim(),
                            OptionC = csv.GetField<string>("OptionC")?.Trim(),
                            OptionD = csv.GetField<string>("OptionD")?.Trim(),
                            OptionE = csv.GetField<string>("OptionE")?.Trim(),
                            OptionF = csv.GetField<string>("OptionF")?.Trim(),
                            CorrectAnswer = csv.GetField<string>("CorrectAnswer")?.Trim().ToUpper() ?? string.Empty
                        };

                        // Validate the row
                        var validationErrors = ValidateQuizQuestionRow(row);
                        if (validationErrors.Any())
                        {
                            errors.AddRange(validationErrors);
                        }
                        else
                        {
                            rows.Add(row);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new QuizImportError
                        {
                            RowNumber = rowNumber,
                            Field = "Row",
                            ErrorMessage = $"Error parsing row: {ex.Message}",
                            RowData = csv.Context.Parser.RawRecord
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new QuizImportError
                {
                    RowNumber = 0,
                    Field = "File",
                    ErrorMessage = $"Error reading CSV file: {ex.Message}",
                    RowData = string.Empty
                });
            }

            return (rows, errors);
        }

        public async Task<(List<AssignmentQuestionCsvRow> rows, List<AssignmentImportError> errors)> ParseAssignmentQuestionsCsvAsync(Stream csvStream)
        {
            var rows = new List<AssignmentQuestionCsvRow>();
            var errors = new List<AssignmentImportError>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = context =>
                {
                    errors.Add(new AssignmentImportError
                    {
                        RowNumber = context.Context.Parser.Row,
                        Field = "Row",
                        ErrorMessage = $"Bad data found: {context.RawRecord}",
                        RowData = context.RawRecord
                    });
                }
            };

            try
            {
                using var reader = new StreamReader(csvStream, Encoding.UTF8);
                using var csv = new CsvReader(reader, config);

                await csv.ReadAsync();
                csv.ReadHeader();

                int rowNumber = 1;
                while (await csv.ReadAsync())
                {
                    rowNumber++;
                    try
                    {
                        var row = new AssignmentQuestionCsvRow
                        {
                            RowNumber = rowNumber,
                            Content = csv.GetField<string>("Content")?.Trim() ?? string.Empty,
                            Points = csv.GetField<decimal>("Points"),
                            AnswerExplanation = csv.GetField<string>("AnswerExplanation")?.Trim(),
                            CriterionA = csv.GetField<string>("CriterionA")?.Trim(),
                            CriterionAMaxPoints = TryGetDecimalField(csv, "CriterionAMaxPoints"),
                            CriterionB = csv.GetField<string>("CriterionB")?.Trim(),
                            CriterionBMaxPoints = TryGetDecimalField(csv, "CriterionBMaxPoints"),
                            CriterionC = csv.GetField<string>("CriterionC")?.Trim(),
                            CriterionCMaxPoints = TryGetDecimalField(csv, "CriterionCMaxPoints"),
                            CriterionD = csv.GetField<string>("CriterionD")?.Trim(),
                            CriterionDMaxPoints = TryGetDecimalField(csv, "CriterionDMaxPoints"),
                            CriterionE = csv.GetField<string>("CriterionE")?.Trim(),
                            CriterionEMaxPoints = TryGetDecimalField(csv, "CriterionEMaxPoints"),
                            CriterionF = csv.GetField<string>("CriterionF")?.Trim(),
                            CriterionFMaxPoints = TryGetDecimalField(csv, "CriterionFMaxPoints")
                        };

                        // Validate the row
                        var validationErrors = ValidateAssignmentQuestionRow(row);
                        if (validationErrors.Any())
                        {
                            errors.AddRange(validationErrors);
                        }
                        else
                        {
                            rows.Add(row);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new AssignmentImportError
                        {
                            RowNumber = rowNumber,
                            Field = "Row",
                            ErrorMessage = $"Error parsing row: {ex.Message}",
                            RowData = csv.Context.Parser.RawRecord
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new AssignmentImportError
                {
                    RowNumber = 0,
                    Field = "File",
                    ErrorMessage = $"Error reading CSV file: {ex.Message}",
                    RowData = string.Empty
                });
            }

            return (rows, errors);
        }

        private decimal? TryGetDecimalField(CsvReader csv, string fieldName)
        {
            try
            {
                var value = csv.GetField<string>(fieldName);
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                return decimal.Parse(value);
            }
            catch
            {
                return null;
            }
        }

        private List<QuizImportError> ValidateQuizQuestionRow(QuizQuestionCsvRow row)
        {
            var errors = new List<QuizImportError>();

            if (string.IsNullOrWhiteSpace(row.Content))
            {
                errors.Add(new QuizImportError
                {
                    RowNumber = row.RowNumber,
                    Field = "Content",
                    ErrorMessage = "Content is required",
                    RowData = $"Row {row.RowNumber}"
                });
            }

            if (row.Points < 0)
            {
                errors.Add(new QuizImportError
                {
                    RowNumber = row.RowNumber,
                    Field = "Points",
                    ErrorMessage = "Points must be a positive number",
                    RowData = $"Row {row.RowNumber}"
                });
            }

            if (string.IsNullOrWhiteSpace(row.OptionA))
            {
                errors.Add(new QuizImportError
                {
                    RowNumber = row.RowNumber,
                    Field = "OptionA",
                    ErrorMessage = "At least Option A is required",
                    RowData = $"Row {row.RowNumber}"
                });
            }

            if (string.IsNullOrWhiteSpace(row.CorrectAnswer))
            {
                errors.Add(new QuizImportError
                {
                    RowNumber = row.RowNumber,
                    Field = "CorrectAnswer",
                    ErrorMessage = "Correct Answer is required",
                    RowData = $"Row {row.RowNumber}"
                });
            }
            else if (!new[] { "A", "B", "C", "D", "E", "F" }.Contains(row.CorrectAnswer))
            {
                errors.Add(new QuizImportError
                {
                    RowNumber = row.RowNumber,
                    Field = "CorrectAnswer",
                    ErrorMessage = "Correct Answer must be A, B, C, D, E, or F",
                    RowData = $"Row {row.RowNumber}"
                });
            }
            else
            {
                // Validate that the correct answer option exists
                var hasCorrectOption = row.CorrectAnswer switch
                {
                    "A" => !string.IsNullOrWhiteSpace(row.OptionA),
                    "B" => !string.IsNullOrWhiteSpace(row.OptionB),
                    "C" => !string.IsNullOrWhiteSpace(row.OptionC),
                    "D" => !string.IsNullOrWhiteSpace(row.OptionD),
                    "E" => !string.IsNullOrWhiteSpace(row.OptionE),
                    "F" => !string.IsNullOrWhiteSpace(row.OptionF),
                    _ => false
                };

                if (!hasCorrectOption)
                {
                    errors.Add(new QuizImportError
                    {
                        RowNumber = row.RowNumber,
                        Field = "CorrectAnswer",
                        ErrorMessage = $"Option {row.CorrectAnswer} is marked as correct but is empty",
                        RowData = $"Row {row.RowNumber}"
                    });
                }
            }

            return errors;
        }

        private List<AssignmentImportError> ValidateAssignmentQuestionRow(AssignmentQuestionCsvRow row)
        {
            var errors = new List<AssignmentImportError>();

            if (string.IsNullOrWhiteSpace(row.Content))
            {
                errors.Add(new AssignmentImportError
                {
                    RowNumber = row.RowNumber,
                    Field = "Content",
                    ErrorMessage = "Content is required",
                    RowData = $"Row {row.RowNumber}"
                });
            }

            if (row.Points < 0)
            {
                errors.Add(new AssignmentImportError
                {
                    RowNumber = row.RowNumber,
                    Field = "Points",
                    ErrorMessage = "Points must be a positive number",
                    RowData = $"Row {row.RowNumber}"
                });
            }

            // Validate criteria
            ValidateCriterion(row, "A", row.CriterionA, row.CriterionAMaxPoints, errors);
            ValidateCriterion(row, "B", row.CriterionB, row.CriterionBMaxPoints, errors);
            ValidateCriterion(row, "C", row.CriterionC, row.CriterionCMaxPoints, errors);
            ValidateCriterion(row, "D", row.CriterionD, row.CriterionDMaxPoints, errors);
            ValidateCriterion(row, "E", row.CriterionE, row.CriterionEMaxPoints, errors);
            ValidateCriterion(row, "F", row.CriterionF, row.CriterionFMaxPoints, errors);

            return errors;
        }

        private void ValidateCriterion(AssignmentQuestionCsvRow row, string letter, string? criterionName,
            decimal? maxPoints, List<AssignmentImportError> errors)
        {
            var hasCriterionName = !string.IsNullOrWhiteSpace(criterionName);
            var hasMaxPoints = maxPoints.HasValue;

            if (hasCriterionName && !hasMaxPoints)
            {
                errors.Add(new AssignmentImportError
                {
                    RowNumber = row.RowNumber,
                    Field = $"Criterion{letter}MaxPoints",
                    ErrorMessage = $"Criterion {letter} has a name but no max points",
                    RowData = $"Row {row.RowNumber}"
                });
            }

            if (!hasCriterionName && hasMaxPoints)
            {
                errors.Add(new AssignmentImportError
                {
                    RowNumber = row.RowNumber,
                    Field = $"Criterion{letter}",
                    ErrorMessage = $"Criterion {letter} has max points but no name",
                    RowData = $"Row {row.RowNumber}"
                });
            }

            if (hasMaxPoints && maxPoints < 0)
            {
                errors.Add(new AssignmentImportError
                {
                    RowNumber = row.RowNumber,
                    Field = $"Criterion{letter}MaxPoints",
                    ErrorMessage = $"Criterion {letter} max points must be positive",
                    RowData = $"Row {row.RowNumber}"
                });
            }
        }
    }
}

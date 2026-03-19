using FluentValidation;
using MediatR;
using Shared.Helper;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Assignment
{
    public class ImportAssignmentQuestionsCommand : IRequest<AssignmentImportResult>
    {
        public int AssignmentId { get; set; }
        public byte[]? CsvFileBytes { get; set; }
    }

    public class ImportAssignmentQuestionsCommandValidator : AbstractValidator<ImportAssignmentQuestionsCommand>
    {
        private const int MaxImageBytes = 5 * 1024 * 1024;

        public ImportAssignmentQuestionsCommandValidator()
        {
            RuleFor(x => x.AssignmentId)
                .GreaterThan(0)
                .WithMessage("AssignmentId must be greater than 0.");

            When(x => x.CsvFileBytes != null, () =>
            {
                RuleFor(x => x.CsvFileBytes)
                    .Must(bytes => bytes != null && bytes.Length > 0)
                    .WithMessage("File is required.")
                    .Must(bytes => bytes != null && bytes.Length <= MaxImageBytes)
                    .WithMessage($"File must not exceed {MaxImageBytes / 1024 / 1024} MB.")
                    .Must(bytes => FileTypeHelper.IsCsv(bytes!))
                    .WithMessage("Invalid csv file format.");
            });
        }
    }
}

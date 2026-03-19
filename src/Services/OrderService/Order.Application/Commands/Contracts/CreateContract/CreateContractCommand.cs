using FluentValidation;
using MediatR;
using Shared.Helper;

namespace Order.Application.Commands.Contracts.CreateContract
{
    public class CreateContractCommand : IRequest<Shared.Protos.Order.GrpcContractDetail>
    {
        public string Name { get; set; }
        public int OrganizationId { get; set; }
        public string? Description { get; set; }
        public byte[]? FileBytes { get; set; }
    }

    public class CreateContractCommandValidator : AbstractValidator<CreateContractCommand>
    {
        private const int MaxImageBytes = 5 * 1024 * 1024;

        public CreateContractCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Contract name is required.")
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Contract name must not be whitespace.")
                .MaximumLength(255)
                .WithMessage("Contract name must not exceed 255 characters.");

            RuleFor(x => x.OrganizationId)
                .GreaterThan(0)
                .WithMessage("OrganizationId must be greater than 0.");

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithMessage("Description must not exceed 2000 characters.")
                .When(x => x.Description != null);

            When(x => x.FileBytes != null, () =>
            {
                RuleFor(x => x.FileBytes)
                    .Must(bytes => bytes != null && bytes.Length > 0)
                    .WithMessage("File is required.")
                    .Must(bytes => bytes != null && bytes.Length <= MaxImageBytes)
                    .WithMessage($"File must not exceed {MaxImageBytes / 1024 / 1024} MB.")
                    .Must(bytes => FileTypeHelper.IsDocument(bytes!))
                    .WithMessage("Invalid docs file format.");
            });
        }
    }
}
using FluentValidation;
using MediatR;
using Order.Domain.Enums;
using Shared.Helper;

namespace Order.Application.Commands.Contracts.UpdateContract
{
    public class UpdateContractCommand : IRequest<Shared.Protos.Order.GrpcContractDetail>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        //public int? OrganizationId { get; set; }
        public string? Description { get; set; }
        public byte[]? FileBytes { get; set; }
        public Domain.Enums.ContractStatus? Status { get; set; }
    }

    public class UpdateContractCommandValidator : AbstractValidator<UpdateContractCommand>
    {
        private const int MaxImageBytes = 5 * 1024 * 1024;

        public UpdateContractCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Contract ID must be greater than 0.");

            // At least one updatable field must be provided
            RuleFor(x => x)
                .Must(cmd => cmd.Name != null
                             //|| cmd.OrganizationId.HasValue
                             || cmd.Description != null
                             || cmd.FileBytes != null
                             || cmd.Status.HasValue)
                .WithMessage("At least one field must be provided to update.");

            When(x => !string.IsNullOrEmpty(x.Name), () =>
            {
                RuleFor(x => x.Name)
                    .Must(name => !string.IsNullOrWhiteSpace(name))
                    .WithMessage("Contract name must not be whitespace.")
                    .MaximumLength(255)
                    .WithMessage("Contract name must not exceed 255 characters.");
            });

            //When(x => x.OrganizationId.HasValue, () =>
            //{
            //    RuleFor(x => x.OrganizationId.Value)
            //        .GreaterThan(0)
            //        .WithMessage("OrganizationId must be greater than 0.");
            //});

            When(x => x.Description != null, () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(2000)
                    .WithMessage("Description must not exceed 2000 characters.");
            });

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

            When(x => x.Status.HasValue, () =>
            {
                RuleFor(x => x.Status.Value)
                    .Must(s => Enum.IsDefined(typeof(ContractStatus), s))
                    .WithMessage("Invalid ContractStatus value.");
            });
        }
    }
}
using FluentValidation;
using MediatR;
using Product.Application.Models;

namespace Product.Application.Features.KitProducts.Commands.CreateKitProduct
{
    public class CreateKitProductCommand : IRequest<Shared.Protos.Product.KitResponse>
    {
        public string Name { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public string? Description { get; set; }
        public string? Dimensions { get; set; }
        public int AgeRangeId { get; set; }
        public List<KitImageUploadDto> Images { get; set; } = new();
        public string CreatedByUserId { get; set; } = string.Empty;
    }

    public class CreateKitProductCommandValidator : AbstractValidator<CreateKitProductCommand>
    {
        public CreateKitProductCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Kit name is required.")
                .MaximumLength(255)
                .WithMessage("Kit name must not exceed 255 characters.");

            RuleForEach(x => x.Images)
                .SetValidator(new KitImageUploadDtoValidator());

            RuleFor(x => x.CreatedByUserId)
                .NotEmpty().WithMessage("CreatedByUserId is required.")
                .Must(BeAValidGuid).WithMessage("CreatedByUserId must be a valid GUID.");
        }

        private bool BeAValidGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }
    }
}

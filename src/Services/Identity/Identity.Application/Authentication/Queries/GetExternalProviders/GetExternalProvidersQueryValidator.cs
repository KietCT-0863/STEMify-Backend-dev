using FluentValidation;

namespace Identity.Application.Authentication.Queries.GetExternalProviders;

/// <summary>
/// Validator for GetExternalProvidersQuery
/// </summary>
public class GetExternalProvidersQueryValidator : AbstractValidator<GetExternalProvidersQuery>
{
    public GetExternalProvidersQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");
    }
}

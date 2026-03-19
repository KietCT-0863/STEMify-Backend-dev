using FluentValidation;

namespace Identity.Application.Users.Queries.GetUsersByType;

/// <summary>
/// Validator for GetUsersByTypeQuery
/// </summary>
public class GetUsersByTypeQueryValidator : AbstractValidator<GetUsersByTypeQuery>
{
    public GetUsersByTypeQueryValidator()
    {
        RuleFor(x => x.UserType)
            .NotEmpty()
            .WithMessage("User type is required")
            .Must(BeValidUserType)
            .WithMessage("User type must be 'student' or 'teacher'");

        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must be between 1 and 100");
    }

    private static bool BeValidUserType(string userType)
    {
        return userType.ToLowerInvariant() is "student" or "teacher";
    }
}

using FluentValidation;
using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Validators;

public static class PasswordRules
{
    /// <summary>Min 8 chars with upper, lower, digit and a symbol. Mirrors userSchema.ts in LearnWEB.</summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty()
        .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
        .MaximumLength(128)
        .Matches("[A-Z]").WithMessage("Password needs an uppercase letter.")
        .Matches("[a-z]").WithMessage("Password needs a lowercase letter.")
        .Matches("[0-9]").WithMessage("Password needs a digit.")
        .Matches("[^A-Za-z0-9]").WithMessage("Password needs a symbol.");
}

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().Matches(@"^[A-Za-z0-9_.-]{3,50}$").WithMessage("3-50 letters, digits, '.', '_' or '-'.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Mobile!).Matches(@"^[6-9]\d{9}$").WithMessage("Enter a valid 10-digit mobile number").When(x => !string.IsNullOrEmpty(x.Mobile));
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.RoleIds).NotEmpty().WithMessage("Select at least one role.");
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Mobile!).Matches(@"^[6-9]\d{9}$").WithMessage("Enter a valid 10-digit mobile number").When(x => !string.IsNullOrEmpty(x.Mobile));
        RuleFor(x => x.RoleIds).NotEmpty().WithMessage("Select at least one role.");
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator() => RuleFor(x => x.NewPassword).StrongPassword();
}

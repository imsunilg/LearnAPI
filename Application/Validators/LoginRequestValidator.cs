using FluentValidation;
using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

using FluentValidation;
using LearnAPI.Application.DTOs;

namespace LearnAPI.Application.Validators;

public sealed class RoleRequestValidator : AbstractValidator<RoleRequest>
{
    public RoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Matches(@"^[A-Za-z][A-Za-z0-9 _-]{1,49}$").WithMessage("2-50 characters, starting with a letter.");
        RuleFor(x => x.Description!).MaximumLength(250).When(x => x.Description is not null);
    }
}

public sealed class SetPermissionsRequestValidator : AbstractValidator<SetPermissionsRequest>
{
    public SetPermissionsRequestValidator() =>
        RuleForEach(x => x.Permissions).Matches(@"^[a-z]+\.[a-z]+$").WithMessage("Invalid permission key.");
}

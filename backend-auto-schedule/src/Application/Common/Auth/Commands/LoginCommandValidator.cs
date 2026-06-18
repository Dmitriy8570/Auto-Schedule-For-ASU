using FluentValidation;

namespace Application.Common.Auth.Commands;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Логин обязателен.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Пароль обязателен.");
    }
}

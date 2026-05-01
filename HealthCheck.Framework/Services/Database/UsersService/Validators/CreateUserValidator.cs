using FluentValidation;
using HealthCheck.Framework.Models;

namespace HealthCheck.Framework.Services.Database.UsersService.Validators;

public class CreateUserValidator : AbstractValidator<User>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MaximumLength(255)
            .WithMessage("Username cannot exceed 255 characters.");
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithName("email is required")
            .When(x => !string.IsNullOrEmpty(x.Email))
            .EmailAddress()
            .WithMessage("Invalid email format.");
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(6)
            .WithMessage("Password must be at least 6 characters long.");
    }
}

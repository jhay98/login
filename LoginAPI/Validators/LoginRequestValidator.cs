using FluentValidation;
using LoginAPI.Models.DTOs;

namespace LoginAPI.Validators;

/// <summary>
/// Validates <see cref="LoginRequestDto"/> payloads.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginRequestValidator"/> class.
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}

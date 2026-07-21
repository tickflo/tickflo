namespace Tickflo.Core.Services.Authentication;

using Tickflo.Core.Exceptions;

public interface IPasswordValidationService
{
    public const int MinLength = 8;

    public void Validate(string? password, string? confirmPassword = null);
}

public class PasswordValidationService : IPasswordValidationService
{
    public void Validate(string? password, string? confirmPassword = null)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new BadRequestException($"Password must be at least {IPasswordValidationService.MinLength} characters long.");
        }

        if (password.Length < IPasswordValidationService.MinLength)
        {
            throw new BadRequestException(
                $"Password must be at least {IPasswordValidationService.MinLength} characters long.");
        }

        if (confirmPassword is not null && !string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            throw new BadRequestException("Passwords do not match.");
        }
    }
}

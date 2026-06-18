namespace Tickflo.Core.Services.Authentication;

public record PasswordValidationResult(bool IsValid, string? ErrorMessage)
{
    public const int MinLength = 8;
}

public interface IPasswordValidationService
{
    public PasswordValidationResult Validate(string? password, string? confirmPassword = null);
}

public class PasswordValidationService : IPasswordValidationService
{
    public PasswordValidationResult Validate(string? password, string? confirmPassword = null)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return new PasswordValidationResult(false, $"Password must be at least {PasswordValidationResult.MinLength} characters long.");
        }

        if (password.Length < PasswordValidationResult.MinLength)
        {
            return new PasswordValidationResult(
                false,
                $"Password must be at least {PasswordValidationResult.MinLength} characters long.");
        }

        if (confirmPassword is not null && !string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return new PasswordValidationResult(false, "Passwords do not match.");
        }

        return new PasswordValidationResult(true, null);
    }
}

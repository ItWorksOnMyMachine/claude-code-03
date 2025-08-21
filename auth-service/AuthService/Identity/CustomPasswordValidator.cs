using AuthService.Data.Entities;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace AuthService.Identity;

/// <summary>
/// Custom password validator that enforces security password policies
/// </summary>
public class CustomPasswordValidator : IPasswordValidator<AppUser>
{
    // Default password requirements
    private const int DefaultMinLength = 8;
    private const bool DefaultRequireUppercase = true;
    private const bool DefaultRequireLowercase = true;
    private const bool DefaultRequireDigit = true;
    private const bool DefaultRequireNonAlphanumeric = false;
    private const int DefaultRequireUniqueChars = 1;

    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password)
    {
        if (password == null)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordRequired",
                Description = "Password is required"
            }));
        }

        var errors = new List<IdentityError>();
        
        // Use standard security rules for all users
        ValidateWithDefaultRules(password, errors);

        return Task.FromResult(errors.Count == 0 
            ? IdentityResult.Success 
            : IdentityResult.Failed(errors.ToArray()));
    }

    private void ValidateWithDefaultRules(string password, List<IdentityError> errors)
    {
        // Check minimum length
        if (password.Length < DefaultMinLength)
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = $"Password must be at least {DefaultMinLength} characters long"
            });
        }

        // Check uppercase requirement
        if (DefaultRequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordRequiresUpper",
                Description = "Password must contain at least one uppercase letter"
            });
        }

        // Check lowercase requirement
        if (DefaultRequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordRequiresLower",
                Description = "Password must contain at least one lowercase letter"
            });
        }

        // Check digit requirement
        if (DefaultRequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordRequiresDigit",
                Description = "Password must contain at least one digit"
            });
        }

        // Check non-alphanumeric requirement
        if (DefaultRequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordRequiresNonAlphanumeric",
                Description = "Password must contain at least one non-alphanumeric character"
            });
        }

        // Check unique characters requirement
        var uniqueChars = password.Distinct().Count();
        if (uniqueChars < DefaultRequireUniqueChars)
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordRequiresUniqueChars",
                Description = $"Password must contain at least {DefaultRequireUniqueChars} unique characters"
            });
        }
    }

}
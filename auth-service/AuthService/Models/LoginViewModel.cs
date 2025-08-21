using AuthService.Controllers;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AuthService.Models;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberLogin { get; set; }
    
    public string? ReturnUrl { get; set; }
    
    public bool AllowRememberLogin { get; set; } = true;
    public bool EnableLocalLogin { get; set; } = true;
    
    public ExternalProvider[] ExternalProviders { get; set; } = Array.Empty<ExternalProvider>();
    
    public string? ExternalLoginScheme => IsExternalLoginOnly ? ExternalProviders?.SingleOrDefault()?.AuthenticationScheme : null;
    
    public bool IsExternalLoginOnly => EnableLocalLogin == false && ExternalProviders?.Length == 1;
}
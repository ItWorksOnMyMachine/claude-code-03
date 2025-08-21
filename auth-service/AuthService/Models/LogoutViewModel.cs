namespace AuthService.Models;

public class LogoutViewModel
{
    public string? LogoutId { get; set; }
    public bool ShowLogoutPrompt { get; set; } = true;
}
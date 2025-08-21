using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Models;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AuthService.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IIdentityServerInteractionService interaction,
        IEventService events,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _interaction = interaction;
        _events = events;
        _logger = logger;
    }

    /// <summary>
    /// Entry point into the login workflow
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl)
    {
        // Build a model so we know what to show on the login page
        var vm = await BuildLoginViewModelAsync(returnUrl);

        if (vm.IsExternalLoginOnly)
        {
            // We only have one option for logging in and it's an external provider
            return RedirectToAction("Challenge", "External", new { scheme = vm.ExternalLoginScheme, returnUrl });
        }

        return View(vm);
    }

    /// <summary>
    /// Handle postback from username/password login
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? button = null)
    {
        // Check if we are in the context of an authorization request
        var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

        // The user clicked the "cancel" button
        if (button == "cancel")
        {
            if (context != null)
            {
                // If the user cancels, send a result back into IdentityServer as if they 
                // denied the consent (even if this client does not require consent).
                // This will send back an access denied OIDC error response to the client.
                await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

                // We can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                if (context.IsNativeClient())
                {
                    // The client is native, so this change in how to
                    // return the response is for better UX for the end user.
                    return this.LoadingPage("Redirect", model.ReturnUrl ?? "~/");
                }

                return Redirect(model.ReturnUrl ?? "~/");
            }
            else
            {
                // Since we don't have a valid context, then we just go back to the home page
                return Redirect("~/");
            }
        }

        if (ModelState.IsValid)
        {
            // Find user by username
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(model.Username);
            }

            if (user != null)
            {
                // Check if user is active
                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Your account is not active. Please contact your administrator.");
                    _logger.LogWarning("Login attempt for inactive user {Username}", model.Username);
                    
                    // Build a model to return
                    var failedVm = await BuildLoginViewModelAsync(model);
                    return View(failedVm);
                }

                // User is active and can proceed to authentication

                // Validate password
                var result = await _signInManager.PasswordSignInAsync(
                    user,
                    model.Password,
                    model.RememberLogin,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    // Update last login time
                    user.LastLoginAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    _logger.LogInformation("User {Username} logged in", model.Username);
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client?.ClientId));

                    if (context != null)
                    {
                        if (context.IsNativeClient())
                        {
                            // The client is native, so this change in how to
                            // return the response is for better UX for the end user.
                            return this.LoadingPage("Redirect", model.ReturnUrl ?? "~/");
                        }

                        // We can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                        return Redirect(model.ReturnUrl ?? "~/");
                    }

                    // Request for a local page
                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else if (string.IsNullOrEmpty(model.ReturnUrl))
                    {
                        return Redirect("~/");
                    }
                    else
                    {
                        // User might have clicked on a malicious link - should be logged
                        _logger.LogWarning("Invalid return URL: {ReturnUrl}", model.ReturnUrl);
                        return Redirect("~/");
                    }
                }

                if (result.RequiresTwoFactor)
                {
                    _logger.LogInformation("User {Username} requires two-factor authentication", model.Username);
                    // TODO: Implement 2FA flow
                    ModelState.AddModelError(string.Empty, "Two-factor authentication is required but not yet implemented.");
                }
                else if (result.IsLockedOut)
                {
                    _logger.LogWarning("User {Username} account locked out", model.Username);
                    ModelState.AddModelError(string.Empty, "Your account has been locked out due to multiple failed login attempts.");
                }
                else if (result.IsNotAllowed)
                {
                    _logger.LogWarning("User {Username} is not allowed to sign in", model.Username);
                    ModelState.AddModelError(string.Empty, "You are not allowed to sign in. Please confirm your email address.");
                }
                else
                {
                    await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials", clientId: context?.Client?.ClientId));
                    ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
                }
            }
            else
            {
                await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials", clientId: context?.Client?.ClientId));
                ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
            }
        }

        // Something went wrong, show form with error
        var vm = await BuildLoginViewModelAsync(model);
        return View(vm);
    }

    /// <summary>
    /// Show logout page
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Logout(string? logoutId)
    {
        // Build a model so the logout page knows what to display
        var vm = await BuildLogoutViewModelAsync(logoutId);

        if (vm.ShowLogoutPrompt == false)
        {
            // If the request for logout was properly authenticated from IdentityServer, then
            // we don't need to show the prompt and can just log the user out directly.
            return await Logout(vm);
        }

        return View(vm);
    }

    /// <summary>
    /// Handle logout page postback
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(LogoutViewModel model)
    {
        // Build a model so the logged out page knows what to display
        var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

        if (User?.Identity?.IsAuthenticated == true)
        {
            // Delete local authentication cookie
            await _signInManager.SignOutAsync();

            // Raise the logout event
            await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
        }

        // Check if we need to trigger sign-out at an upstream identity provider
        if (vm.TriggerExternalSignout)
        {
            // Build a return URL so the upstream provider will redirect back
            // to us after the user has logged out. This allows us to then
            // complete our single sign-out processing.
            string? url = Url.Action("Logout", new { logoutId = vm.LogoutId });

            // This triggers a redirect to the external provider for sign-out
            return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme!);
        }

        return View("LoggedOut", vm);
    }

    /*****************************************/
    /* helper APIs for the AccountController */
    /*****************************************/
    private async Task<LoginViewModel> BuildLoginViewModelAsync(string? returnUrl)
    {
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        
        if (context?.IdP != null && await GetSchemeProviderAsync(context.IdP) != null)
        {
            var local = context.IdP == Duende.IdentityServer.IdentityServerConstants.LocalIdentityProvider;

            // This is meant to short circuit the UI and only trigger the one external IdP
            var vm = new LoginViewModel
            {
                EnableLocalLogin = local,
                ReturnUrl = returnUrl,
                Username = context.LoginHint ?? string.Empty,
            };

            if (!local)
            {
                vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
            }

            return vm;
        }

        var schemes = await GetSchemesAsync();

        var providers = schemes
            .Where(x => x.DisplayName != null)
            .Select(x => new ExternalProvider
            {
                DisplayName = x.DisplayName ?? x.Name,
                AuthenticationScheme = x.Name
            }).ToList();

        var allowLocal = true;
        if (context?.Client?.ClientId != null)
        {
            var client = await GetClientAsync(context.Client.ClientId);
            if (client != null)
            {
                allowLocal = client.EnableLocalLogin;

                if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                {
                    providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                }
            }
        }

        return new LoginViewModel
        {
            AllowRememberLogin = AccountOptions.AllowRememberLogin,
            EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
            ReturnUrl = returnUrl,
            Username = context?.LoginHint ?? string.Empty,
            ExternalProviders = providers.ToArray()
        };
    }

    private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginViewModel model)
    {
        var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
        vm.Username = model.Username;
        vm.RememberLogin = model.RememberLogin;
        return vm;
    }

    private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string? logoutId)
    {
        var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

        if (User?.Identity?.IsAuthenticated != true)
        {
            // If the user is not authenticated, then just show logged out page
            vm.ShowLogoutPrompt = false;
            return vm;
        }

        var context = await _interaction.GetLogoutContextAsync(logoutId);
        if (context?.ShowSignoutPrompt == false)
        {
            // It's safe to automatically sign-out
            vm.ShowLogoutPrompt = false;
            return vm;
        }

        // Show the logout prompt. This prevents attacks where the user
        // is automatically signed out by another malicious web page.
        return vm;
    }

    private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string? logoutId)
    {
        // Get context information (client name, post logout redirect URI and iframe for federated signout)
        var logout = await _interaction.GetLogoutContextAsync(logoutId);

        var vm = new LoggedOutViewModel
        {
            AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
            PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
            ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
            SignOutIframeUrl = logout?.SignOutIFrameUrl,
            LogoutId = logoutId
        };

        if (User?.Identity?.IsAuthenticated == true)
        {
            var idp = User.FindFirst("idp")?.Value;
            if (idp != null && idp != Duende.IdentityServer.IdentityServerConstants.LocalIdentityProvider)
            {
                var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                if (providerSupportsSignout)
                {
                    if (vm.LogoutId == null)
                    {
                        // If there's no current logout context, we need to create one
                        // this captures necessary info from the current logged in user
                        // before we signout and redirect away to the external IdP for signout
                        vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                    }

                    vm.ExternalAuthenticationScheme = idp;
                }
            }
        }

        return vm;
    }

    private async Task<IEnumerable<AuthenticationScheme>> GetSchemesAsync()
    {
        var schemes = await HttpContext.RequestServices
            .GetRequiredService<IAuthenticationSchemeProvider>()
            .GetAllSchemesAsync();

        return schemes.Where(x => x.DisplayName != null);
    }

    private async Task<AuthenticationScheme?> GetSchemeProviderAsync(string provider)
    {
        var schemeProvider = HttpContext.RequestServices
            .GetRequiredService<IAuthenticationSchemeProvider>();
        
        return await schemeProvider.GetSchemeAsync(provider);
    }

    private async Task<Client?> GetClientAsync(string clientId)
    {
        var clientStore = HttpContext.RequestServices
            .GetRequiredService<IClientStore>();
        
        return await clientStore.FindClientByIdAsync(clientId);
    }
}

// Account options configuration
public static class AccountOptions
{
    public static bool AllowLocalLogin = true;
    public static bool AllowRememberLogin = true;
    public static TimeSpan RememberMeLoginDuration = TimeSpan.FromDays(30);

    public static bool ShowLogoutPrompt = true;
    public static bool AutomaticRedirectAfterSignOut = false;

    public static string InvalidCredentialsErrorMessage = "Invalid username or password";
}

// External provider model
public class ExternalProvider
{
    public string DisplayName { get; set; } = string.Empty;
    public string AuthenticationScheme { get; set; } = string.Empty;
}

// Extension methods
public static class Extensions
{
    /// <summary>
    /// Checks if the redirect URI is for a native client.
    /// </summary>
    public static bool IsNativeClient(this AuthorizationRequest context)
    {
        return !string.IsNullOrEmpty(context.RedirectUri) &&
            !context.RedirectUri.StartsWith("https", StringComparison.Ordinal) &&
            !context.RedirectUri.StartsWith("http", StringComparison.Ordinal);
    }

    public static IActionResult LoadingPage(this Controller controller, string viewName, string redirectUri)
    {
        controller.HttpContext.Response.StatusCode = 200;
        controller.HttpContext.Response.Headers["Location"] = "";
        
        return controller.View(viewName, new RedirectViewModel { RedirectUrl = redirectUri });
    }

    public static async Task<bool> GetSchemeSupportsSignOutAsync(this HttpContext context, string scheme)
    {
        var provider = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
        var handler = await provider.GetHandlerAsync(context, scheme);
        return (handler is IAuthenticationSignOutHandler);
    }
}

// Redirect view model
public class RedirectViewModel
{
    public string RedirectUrl { get; set; } = string.Empty;
}
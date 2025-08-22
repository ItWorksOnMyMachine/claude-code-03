using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace PlatformBff.Tests.Authentication;

public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public bool IsAuthenticated { get; set; } = false;
}

public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Options.IsAuthenticated)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }
}
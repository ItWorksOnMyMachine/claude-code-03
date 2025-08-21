using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AuthService.Tests.Helpers
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>();
            
            // Check for test authorization header
            if (Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                
                if (authHeader == "Test Admin")
                {
                    claims.Add(new Claim(ClaimTypes.Name, "testadmin@example.com"));
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, "test-admin-id"));
                    claims.Add(new Claim(ClaimTypes.Email, "testadmin@example.com"));
                    claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                }
                else if (authHeader == "Test User")
                {
                    claims.Add(new Claim(ClaimTypes.Name, "testuser@example.com"));
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, "test-user-id"));
                    claims.Add(new Claim(ClaimTypes.Email, "testuser@example.com"));
                    claims.Add(new Claim(ClaimTypes.Role, "User"));
                }
                else
                {
                    return Task.FromResult(AuthenticateResult.Fail("Invalid test authorization header"));
                }
            }
            else
            {
                // No authentication header - anonymous user
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
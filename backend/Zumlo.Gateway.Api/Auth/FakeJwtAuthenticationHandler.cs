
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Zumlo.Gateway.Api.Auth
{
    public class FakeJwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "FakeJwt";
        public FakeJwtAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));

            var auth = Request.Headers["Authorization"].ToString();
            if (!auth.StartsWith("Bearer "))
                return Task.FromResult(AuthenticateResult.Fail("Invalid scheme"));

            var token = auth.Substring("Bearer ".Length);
            if (string.IsNullOrWhiteSpace(token))
                return Task.FromResult(AuthenticateResult.Fail("Invalid token"));

            // Accept any non-empty token for exercise purposes
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user"), new Claim(ClaimTypes.Name, "exercise-user") }, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

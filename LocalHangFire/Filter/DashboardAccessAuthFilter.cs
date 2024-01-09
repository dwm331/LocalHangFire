using Hangfire.Dashboard;
using Hybrid.Helper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Core;
using System.Net;

namespace LocalHangFire.Filter
{
    public class DashboardAccessAuthFilter : IDashboardAuthorizationFilter
    {
        private IConfiguration _configuration { get; }

        private const string HangFireCookieName = "HangFireCookie";
        private const int CookieExpirationMinutes = 60;

        private TokenValidationParameters _tokenValidationParameters;
        private string? _role;

        public DashboardAccessAuthFilter(IConfiguration configuration, TokenValidationParameters tokenValidationParameters, string? role = null)
        {
            _configuration = configuration;
            _tokenValidationParameters = tokenValidationParameters;
            _role = role;
        }

        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            var access_token = GetAccessTokenFromRequest(httpContext);

            if (String.IsNullOrEmpty(access_token))
            {
                return false;
            }

            try
            {
                JwtHelpers jwtHelpers = new JwtHelpers(_configuration);
                if (!jwtHelpers.VerifyToken(access_token, _tokenValidationParameters, _role))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error during dashboard Hangfire JWT validation process: {e.Message}");
                throw;
            }

            // 存取Cookie
            SetAccessTokenCookie(httpContext, access_token);

            return true;
        }

        private string? GetAccessTokenFromRequest(HttpContext httpContext)
        {
            if (httpContext.Request.Query.TryGetValue("access_token", out var accessTokenFromQuery))
            {
                return accessTokenFromQuery.FirstOrDefault();
            }

            return httpContext.Request.Cookies[HangFireCookieName];
        }

        private void SetAccessTokenCookie(HttpContext httpContext, string accessToken)
        {
            httpContext.Response.Cookies.Append(HangFireCookieName,
                accessToken,
                new CookieOptions
                {
                    Expires = DateTime.Now.AddMinutes(CookieExpirationMinutes)
                });
        }

    }

}

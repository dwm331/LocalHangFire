﻿using Hangfire.Dashboard;
using Hybrid.Helper;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Net;

namespace LocalHangFire.Filter
{
    public class DashboardAccessAuthFilter : IDashboardAuthorizationFilter
    {
        private IConfiguration _configuration { get; }

        private string HangFireCookieName;
        private int CookieExpirationMinutes;

        private TokenValidationParameters _tokenValidationParameters;
        private string? _role;

        public DashboardAccessAuthFilter(IConfiguration configuration, TokenValidationParameters tokenValidationParameters, string? role = null)
        {
            _configuration = configuration;
            _tokenValidationParameters = tokenValidationParameters;
            _role = role;

            HangFireCookieName = configuration.GetValue<string>("HangfireConfig:HangFireCookieName", "HangFireCookie")!;
            CookieExpirationMinutes = configuration.GetValue<int>("HangfireConfig:CookieExpirationMinutes", 60);
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
                Log.Logger.Error($"Error during dashboard Hangfire JWT validation process: {e.Message}");
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
        public static bool IsLocal(DashboardContext context)
        {
            if (IPAddress.TryParse(context.Request.RemoteIpAddress, out var clientIp))
            {
                // 使用 IPAddress.IsLoopback 屬性檢查是否是本機地址
                var isLocal = IPAddress.IsLoopback(clientIp);
                Log.Logger.Debug("isLocal " + isLocal);
                // 依據來源 IP、登入帳號決定可否存取
                // 例如：非本機存取只能讀取
                return !isLocal;
            }

            // 如果解析失敗，視情況返回 true 或 false
            return true;
        }
    }

}

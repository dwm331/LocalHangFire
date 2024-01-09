using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LocalHangFire.Dtos;
using Hybrid.Helper;
using LocalHangFire.Services;

namespace LocalHangFire.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private IConfiguration _config { get; }
        private JwtHelpers _jwt { get; set; }

        private readonly ILogger<AccountController> _logger;

        public AccountController(IConfiguration configuration, JwtHelpers jwt, ILogger<AccountController> logger)
        {
            _config = configuration;
            _jwt = jwt;
            _logger = logger;
        }

        /// <summary>
        ///  登入
        /// </summary>
        /// <param name="username">帳號</param>
        /// <param name="password">密碼</param>
        /// <returns></returns>
        /// <response code="200">登入Token</response>
        /// <response code="503">系統過載或維護中</response>
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [HttpPost("Login")]
        public IActionResult BackendLogin([FromForm] string username, [FromForm] string password)
        {
            Dictionary<string, AccountListBatchBase> _accountBatchs = _config.GetSection("HangfireConfig:AccountList").Get<Dictionary<string, AccountListBatchBase>>()!;
            LoginResponseBase rep = new LoginResponseBase();
            foreach (KeyValuePair<string, AccountListBatchBase> user in _accountBatchs)
            {
                if (user.Value.account!.Equals(username) && user.Value.password!.Equals(password))
                {
                    TimeSpan expires1 = TimeSpan.FromHours(8);
                    IList<Claim> claims1 = new List<Claim>();
                    claims1.Add(new Claim("roles", user.Value.character!.ToString())); // 角色規則
                    string access_token = _jwt.CreateTpJwt(user.Value.account, expires1, claims1);
                    rep.success = true;
                    rep.data = access_token;
                    return Ok(rep);
                }
            }
            rep.message = "登入失敗";
            return Ok(rep);
        }
    }
}

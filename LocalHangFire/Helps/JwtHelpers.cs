using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Hybrid.Helper
{
    public class JwtHelpers
    {
        private readonly IConfiguration Configuration;

        public JwtHelpers(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }
        public string GenerateToken(string userName, int expireMinutes = 30)
        {
            var issuer = Configuration.GetValue<string>("JwtSettings:Issuer");
            var signKey = Configuration.GetValue<string>("JwtSettings:SignKey");

            // 設定要加入到 JWT Token 中的聲明資訊(Claims)
            var claims = new List<Claim>();

            // 在 RFC 7519 規格中(Section#4)，總共定義了 7 個預設的 Claims，我們應該只用的到兩種！
            //claims.Add(new Claim(JwtRegisteredClaimNames.Iss, issuer));
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userName)); // User.Identity.Name
            //claims.Add(new Claim(JwtRegisteredClaimNames.Aud, "The Audience"));
            //claims.Add(new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds().ToString()));
            //claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())); // 必須為數字
            //claims.Add(new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())); // 必須為數字
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())); // JWT ID

            // 網路上常看到的這個 NameId 設定是多餘的
            //claims.Add(new Claim(JwtRegisteredClaimNames.NameId, userName));

            // 這個 Claim 也以直接被 JwtRegisteredClaimNames.Sub 取代，所以也是多餘的
            //claims.Add(new Claim(ClaimTypes.Name, userName));

            // 你可以自行擴充 "roles" 加入登入者該有的角色
            claims.Add(new Claim("roles", "Admin"));
            claims.Add(new Claim("roles", "Users"));

            var userClaimsIdentity = new ClaimsIdentity(claims);

            // 建立一組對稱式加密的金鑰，主要用於 JWT 簽章之用
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey!));

            // HmacSha256 有要求必須要大於 128 bits，所以 key 不能太短，至少要 16 字元以上
            // https://stackoverflow.com/questions/47279947/idx10603-the-algorithm-hs256-requires-the-securitykey-keysize-to-be-greater
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            // 建立 SecurityTokenDescriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                //Audience = issuer, // 由於你的 API 受眾通常沒有區分特別對象，因此通常不太需要設定，也不太需要驗證
                //NotBefore = DateTime.Now, // 預設值就是 DateTime.Now
                //IssuedAt = DateTime.Now, // 預設值就是 DateTime.Now
                Subject = userClaimsIdentity,
                Expires = DateTime.Now.AddMinutes(expireMinutes),
                //Expires = DateTime.Now.AddYears(2),
                SigningCredentials = signingCredentials
            };

            // 產出所需要的 JWT securityToken 物件，並取得序列化後的 Token 結果(字串格式)
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var serializeToken = tokenHandler.WriteToken(securityToken);

            return serializeToken;
        }

        public static JwtSecurityToken GetJwtToken(
            string username,
            string signingKey,
            string issuer,
            string? audience,
            TimeSpan expiration,
            Claim[]? additionalClaims = null)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,username),
                // this guarantees the token is unique
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (additionalClaims is not null)
            {
                var claimList = new List<Claim>(claims);
                claimList.AddRange(additionalClaims);
                claims = claimList.ToArray();
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            return new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                expires: DateTime.UtcNow.Add(expiration),
                claims: claims,
                signingCredentials: creds
            );
        }

        public string CreateTpJwt(string username, TimeSpan expires, IEnumerable<Claim> claims)
        {
            string issuer = Configuration.GetValue<string>("JwtSettings:Issuer")!;
            string signKey = Configuration.GetValue<string>("JwtSettings:SignKey")!;

            JwtSecurityToken jwtToekn = GetJwtToken(username, signKey, issuer, null, expires, claims?.ToArray());

            return new JwtSecurityTokenHandler().WriteToken(jwtToekn);
        }

        /// <summary>
        /// 校驗token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="principal"></param>
        /// <returns></returns>
        public bool VerifyToken(string token, TokenValidationParameters tokenValidationParameters, string? role)
        {
            try
            {
                //校驗並解析token,validatedToken是解密後的對象
                var handler = new JwtSecurityTokenHandler();
                var claims = handler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

                if (!String.IsNullOrEmpty(role) && !claims.IsInRole(role))
                {
                    return false;
                }
                return true;
            }
            catch (SecurityTokenExpiredException ex)
            {
                //表示過期
                Log.Logger.Debug($"[VerifyTpToken] token 過期 {ex.Message},({token})");
                return false;
            }
            catch (SecurityTokenException ex)
            {
                //表示token錯誤
                Log.Logger.Debug($"[VerifyTpToken] token 錯誤 {ex.Message},({token})");
                return false;
            }
            catch (Exception ex)
            {
                Log.Logger.Debug($"[VerifyTpToken] 其他錯誤 {ex.Message},({token})");
                return false;
            }
        }
    }
}

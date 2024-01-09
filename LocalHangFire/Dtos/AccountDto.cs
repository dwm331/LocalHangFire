using System.ComponentModel.DataAnnotations;

namespace LocalHangFire.Dtos
{
    public class AccountDto
    {
        public class LoginViewModel
        {
            [Required(ErrorMessage = "請輸入使用者名稱")]
            public string? Username { get; set; }

            [Required(ErrorMessage = "請輸入密碼")]
            [DataType(DataType.Password)]
            public string? Password { get; set; }
        }
    }

    public class AccountListBatchBase
    {

        /// <summary>
        /// 角色
        /// </summary>
        public string? character { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        public string? name { get; set; }

        /// <summary>
        /// 帳號
        /// </summary>
        public string? account { get; set; }

        /// <summary>
        /// 密碼
        /// </summary>
        public string? password { get; set; }

        /// <summary>
        /// 是否啟用
        /// </summary>
        public int enable { get; set; }
    }
    public class BackendResponseBase
    {
        public bool success { get; set; } = false;

        public string? message { get; set; }
    }

    public class LoginResponseBase : BackendResponseBase
    {
        public string? data { get; set; }
    }

}

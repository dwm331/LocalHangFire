using Hangfire;
using Hangfire.Dashboard;
using Hybrid.Helper;
using LocalHangFire.Filter;
using LocalHangFire.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

IConfiguration configuration = builder.Configuration;

// Add Hangfire
builder.Services.AddHangfire(x => x.UseSqlServerStorage(configuration.GetValue<string>("DatabaseSettings:HangFireDB")));
builder.Services.AddHangfireServer();

TokenValidationParameters _customTokenValidationParameters = new TokenValidationParameters
{
    // 透過這項宣告，就可以從 "sub" 取值並設定給 User.Identity.Name
    NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
    // 透過這項宣告，就可以從 "roles" 取值，並可讓 [Authorize] 判斷角色
    RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",

    // 一般我們都會驗證 Issuer
    ValidateIssuer = true,
    ValidIssuer = configuration.GetValue<string>("JwtSettings:Issuer"),

    // 通常不太需要驗證 Audience
    ValidateAudience = false,
    //ValidAudience = "JwtAuthDemo", // 不驗證就不需要填寫

    // 一般我們都會驗證 Token 的有效期間
    ValidateLifetime = true, // 無須用戶登入，採固定制
    ClockSkew = TimeSpan.Zero, //校驗過期時間必須加此屬性

    // 如果 Token 中包含 key 才需要驗證，一般都只有簽章而已
    ValidateIssuerSigningKey = false,

    // "1234567890123456" 應該從 IConfiguration 取得
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("JwtSettings:SignKey")!))
};

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        // 當驗證失敗時，回應標頭會包含 WWW-Authenticate 標頭，這裡會顯示失敗的詳細錯誤原因
                        options.IncludeErrorDetails = true; // 預設值為 true，有時會特別關閉

                        options.TokenValidationParameters = _customTokenValidationParameters;
                    });

builder.Services.AddScoped<SimpleJobService>();
builder.Services.AddSingleton<JwtHelpers>();

var logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 加入認證及授權中介軟體
app.UseAuthentication();
app.UseAuthorization();

// 啟用靜態檔案
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "StaticFiles")),
    RequestPath = "/account",
    OnPrepareResponse = ctx =>
    {
        // 如果有下載檔案，在更詳盡的設定路徑
        if (ctx.Context.Request.Path.StartsWithSegments("/account"))
        {
            ctx.Context.Response.Headers.Add("Cache-Control", "no-store;no-cache");
        }
    }
});

//app.UseHangfireDashboard();

var options = new DashboardOptions
{
    Authorization = new IDashboardAuthorizationFilter[]
        {
            new DashboardAccessAuthFilter(configuration, _customTokenValidationParameters, "Admin")
        },
    AppPath = "https://localhost:7125/account/Login.html"
};
app.UseHangfireDashboard("/hangfire", options);

app.MapControllers();

app.Run();

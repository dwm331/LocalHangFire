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
    // �z�L�o���ŧi�A�N�i�H�q "sub" ���Ȩó]�w�� User.Identity.Name
    NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
    // �z�L�o���ŧi�A�N�i�H�q "roles" ���ȡA�åi�� [Authorize] �P�_����
    RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",

    // �@��ڭ̳��|���� Issuer
    ValidateIssuer = true,
    ValidIssuer = configuration.GetValue<string>("JwtSettings:Issuer"),

    // �q�`���ӻݭn���� Audience
    ValidateAudience = false,
    //ValidAudience = "JwtAuthDemo", // �����ҴN���ݭn��g

    // �@��ڭ̳��|���� Token �����Ĵ���
    ValidateLifetime = true, // �L���Τ�n�J�A�ĩT�w��
    ClockSkew = TimeSpan.Zero, //����L���ɶ������[���ݩ�

    // �p�G Token ���]�t key �~�ݭn���ҡA�@�볣�u��ñ���Ӥw
    ValidateIssuerSigningKey = false,

    // "1234567890123456" ���ӱq IConfiguration ���o
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("JwtSettings:SignKey")!))
};

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        // �����ҥ��ѮɡA�^�����Y�|�]�t WWW-Authenticate ���Y�A�o�̷|��ܥ��Ѫ��Բӿ��~��]
                        options.IncludeErrorDetails = true; // �w�]�Ȭ� true�A���ɷ|�S�O����

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

// �[�J�{�Ҥα��v�����n��
app.UseAuthentication();
app.UseAuthorization();

// �ҥ��R�A�ɮ�
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "StaticFiles")),
    RequestPath = "/account",
    OnPrepareResponse = ctx =>
    {
        // �p�G���U���ɮסA�b��Ժɪ��]�w���|
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

using Hangfire;
using LocalHangFire.Services;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

IConfiguration configuration = builder.Configuration;

// Add Hangfire
builder.Services.AddHangfire(x => x.UseSqlServerStorage("server=localhost\\SQLEXPRESS;uid=sam;pwd=123456;database=LocalHangfire;TrustServerCertificate=True;"));
builder.Services.AddHangfireServer();

builder.Services.AddScoped<SimpleJobService>();

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
    app.UseHangfireDashboard();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

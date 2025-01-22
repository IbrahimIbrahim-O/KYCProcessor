using Dapper;
using KYCProcessor.Api.Dapper;
using KYCProcessor.Api.Helpers;
using KYCProcessor.Api.Interfaces;
using KYCProcessor.Api.Services;
using KYCProcessor.Data;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Enums;
using KYCProcessor.Data.Models;
using KYCProcessor.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection.Metadata;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTransient<IDapperService, DapperService>();
builder.Services.AddTransient<ISignUpService, SignUpService>();
builder.Services.AddTransient<ISignUpAdminService, SignUpAdminService>();
builder.Services.AddTransient<IConfirmKycService, ConfirmKycService>();
builder.Services.AddTransient<ILoginService, LoginService>();
builder.Services.AddTransient<ISubmitKycService, SubmitKycService>();
builder.Services.AddTransient<IRejectKycService, RejectKycService>();


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));


var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthentication();
    app.UseAuthorization();
}


#region MINIMAL APIS

app.MapPost("/signup", async (SignUpRequest request, ISignUpService _signUpService) =>
{
    var result = await _signUpService.HandleSignUp(request);
    return result;
});


app.MapPost("/signupAdmin", async (SignUpAdminRequest request, ISignUpAdminService _signUpAdminService) =>
{
    var result = await _signUpAdminService.HandleAdminSignUp(request);
    return result;
});


app.MapPost("/login", async (LoginUserRequest request, ILoginService _loginService) =>
{
    var result = await _loginService.LoginAsync(request);
    return result;
});


app.MapPost("/submitKycForm", [Authorize] async (SubmitKycFormRequest request, ISubmitKycService _submitKycService) =>
{
    var result = await _submitKycService.SubmitKycFormAsync(request);
    return result;
});

app.MapPost("/confirmKycForm", [Authorize(Policy = "AdminOnly")] async (ConfirmKycFormRequest request, IConfirmKycService _confirmKycService) =>
{
    var result = await _confirmKycService.ConfirmKycFormAsync(request);
    return result;
});


app.MapPost("/rejectKycForm", [Authorize(Policy = "AdminOnly")] async (RejectKycFormRequest request, IRejectKycService _rejectKycService) =>
{
    var result = await _rejectKycService.RejectKycFormAsync(request);
    return result;
});

#endregion


app.Run();

public partial class Program { }
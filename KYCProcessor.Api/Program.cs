using KYCProcessor.Api.Helpers;
using KYCProcessor.Data;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Enums;
using KYCProcessor.Data.Models;
using KYCProcessor.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddInfrastructure(builder.Configuration);

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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

#region MINIMAL APIS

app.MapPost("/signup", async (SignUpRequest request, AppDbContext dbContext) =>
{
    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(request);

    bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

    if (!isValid)
    {
        return Results.BadRequest(validationResults); 
    }


    if (request == null)
    {
        return Results.BadRequest(new
        {
            message = "Request cannot be null"
        });
    }

    var existingUser = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

    if (existingUser != null)
    {
        return Results.BadRequest(new
        {
            message = "User with this email already exists."
        });
    }

    var existingPhoneNumber = await dbContext.Users.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

    if (existingPhoneNumber != null)
    {
        return Results.BadRequest(new
        {
            message = "User with this phone number already exists."
        });
    }

    var (hashedPassword, salt) = PasswordHash.HashPassword(request.Password);

    var user = new User
    {
        Id = Guid.NewGuid(),
        Email = request.Email,
        FirstName = request.FirstName,
        LastName = request.LastName,
        Gender = request.Gender,
        PhoneNumber = request.PhoneNumber,
        HashedPassword = hashedPassword,
        PasswordSalt = salt,
        CreatedAt = DateTime.UtcNow
    };

    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();

    var jwtToken = new JwtToken(builder.Configuration);
    var token = jwtToken.GenerateJwtToken(user);

    return Results.Ok(new { Token = token });
});

app.MapPost("/signupAdmin", async (SignUpRequest request, AppDbContext dbContext) =>
{
    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(request);

    bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

    if (!isValid)
    {
        return Results.BadRequest(validationResults);
    }

    if (request == null)
    {
        return Results.BadRequest(new
        {
            message = "Request cannot be null"
        });
    }

    var existingUser = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

    if (existingUser != null)
    {
        return Results.BadRequest(new
        {
            message = "User with this email already exists."
        });
    }

    var existingPhoneNumber = await dbContext.Users.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

    if (existingPhoneNumber != null)
    {
        return Results.BadRequest(new
        {
            message = "User with this phone number already exists."
        });
    }

    var (hashedPassword, salt) = PasswordHash.HashPassword(request.Password);

    var user = new User
    {
        Id = Guid.NewGuid(),
        Email = request.Email,
        FirstName = request.FirstName,
        LastName = request.LastName,
        Gender = request.Gender,
        PhoneNumber = request.PhoneNumber,
        HashedPassword = hashedPassword,
        PasswordSalt = salt,
        CreatedAt = DateTime.UtcNow,
        Role = "admin"
    };

    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();

    var jwtToken = new JwtToken(builder.Configuration);
    var token = jwtToken.GenerateJwtToken(user);

    return Results.Ok(new { Token = token });
});

app.MapPost("/login", async (LoginUserRequest request, AppDbContext dbContext) =>
{
    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(request);

    bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

    if (!isValid)
    {
        return Results.BadRequest(validationResults);
    }

    if (request == null)
    {
        return Results.BadRequest(new
        {
            message = "Request cannot be null"
        });
    }

    if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
    {
        return Results.BadRequest(new
        {
            message = "Email and password are required."
        });
    }

    var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

    if (user == null)
    {
        return Results.BadRequest(new
        {
            message = "Invalid credentials."
        });
    }

    var hashedPassword = PasswordHash.HashPasswordWithSalt(request.Password, user.PasswordSalt);

    if (hashedPassword != user.HashedPassword)
    {
        return Results.BadRequest(new
        {
            message = "Invalid credentials."
        });
    }

    user.LastLoginAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();

    var jwtToken = new JwtToken(builder.Configuration);  
    var token = jwtToken.GenerateJwtToken(user);

    var userInfo = new
    {
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Id = user.Id,
        PhoneNumber = user.PhoneNumber
    };

    return Results.Ok(new { Token = token, UserInfo = userInfo});
});

app.MapPost("/submitKycForm",  [Authorize] async (SubmitKycFormRequest request, AppDbContext dbContext) =>
{
    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(request);

    bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

    if (!isValid)
    {
        return Results.BadRequest(validationResults);
    }

    if (request == null)
    {
        return Results.BadRequest(new
        {
            message = "Request cannot be null"
        });
    }

    var kycRequestPending = await dbContext.KycForms.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.KycStatus == KycStatus.Pending);

    if (kycRequestPending != null)
    {
        return Results.BadRequest(new
        {
            message = "You currently have a pending KYC request, Our people are currently reviewing your information and you will get a response shortly."
        });
    }

    var kycRequestConfirmed = await dbContext.KycForms.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.KycStatus == KycStatus.Confirmed);

    if (kycRequestConfirmed != null)
    {
        return Results.BadRequest(new
        {
            message = "Your Kyc form has been confirmed."
        });
    }

    var kycForm = new KycForm
    {
        Id = Guid.NewGuid(),
        PhoneNumber = request.PhoneNumber,
        Name = request.FirstName,
        CreatedAt = DateTime.UtcNow
    };

    dbContext.KycForms.Add(kycForm);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new
    {
        message = "Kyc form submitted sucessfully, our team will review it and respond within 24 hours"
    });
});

app.MapPost("/confirmKycForm", [Authorize(Policy = "AdminOnly")] async (ConfirmKycFormRequest request, AppDbContext dbContext) =>
{
    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(request);

    bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

    if (!isValid)
    {
        return Results.BadRequest(validationResults);
    }

    if (request == null)
    {
        return Results.BadRequest(new
        {
            message = "Request cannot be null"
        });
    }

    var kycRequestApproved= await dbContext
                .KycForms.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.KycStatus == KycStatus.Confirmed);

    if (kycRequestApproved != null)
    {
        return Results.BadRequest(new
        {
            message = "This customer already has a confirmed KYC"
        });
    }

    var kycRequestToConfiram = await dbContext.KycForms.ToListAsync();


    var kycRequestToConfirm = await dbContext.KycForms
                    .SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.KycStatus == KycStatus.Pending);

    if (kycRequestToConfirm == null)
    {
        return Results.BadRequest(new
        {
            message = "Kyc form does not exist."
        });
    }

    kycRequestToConfirm.KycStatus = KycStatus.Confirmed;

    var userCredit = await dbContext.UserCredits
       .SingleOrDefaultAsync(uc => uc.PhoneNumber == request.PhoneNumber && uc.CreditStatus == CreditStatus.Credited);

    if (userCredit != null)
    {
        return Results.BadRequest(new { message = "This customer has already received the 200 Naira credit." });
    }

    var newCredit = new UserCredit
    {
        Id = Guid.NewGuid(),
        PhoneNumber = request.PhoneNumber,
        Amount = 200,
        CreditStatus = CreditStatus.Credited,
        CreatedAt = DateTime.UtcNow
    };

    await dbContext.UserCredits.AddAsync(newCredit);

    await dbContext.SaveChangesAsync();

    return Results.Ok(new
    { 
        message = "Kyc form confirmed and customer credited with 200 naira." 
    });

});


#endregion


app.Run();

public partial class Program { }
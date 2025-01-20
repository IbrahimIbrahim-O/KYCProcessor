using KYCProcessor.Api.Helpers;
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



#region MINIMAL APIS

app.MapPost("/signup", async (SignUpRequest request, AppDbContext dbContext, ILogger<Program> logger) =>
{
    logger.LogInformation("Handling /signup request for email: {Email}", request.Email);

    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(request);

    bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

    if (!isValid)
    {
        logger.LogWarning("Validation failed for /signup request: {ValidationResults}", validationResults);
        return Results.BadRequest(validationResults); 
    }


    if (request == null)
    {
        logger.LogError("Signup request cannot be null.");
        return Results.BadRequest(new
        {
            message = "Request cannot be null"
        });
    }

    var existingUser = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

    if (existingUser != null)
    {
        logger.LogWarning("User with email {Email} already exists.", request.Email);
        return Results.BadRequest(new
        {
            message = "User with this email already exists."
        });
    }

    var existingPhoneNumber = await dbContext.Users.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

    if (existingPhoneNumber != null)
    {
        logger.LogWarning("User with phone number {PhoneNumber} already exists.", request.PhoneNumber);
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

    logger.LogInformation("New user with email {Email} created successfully.", request.Email);

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

app.MapPost("/login", async (LoginUserRequest request, AppDbContext dbContext, ILogger<Program> logger) =>
{
    logger.LogInformation("Handling /login request for email: {Email}", request.Email);

    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(request);

    bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

    if (!isValid)
    {
        logger.LogWarning("Validation failed for /login request: {ValidationResults}", validationResults);
        return Results.BadRequest(validationResults);
    }

    if (request == null)
    {
        logger.LogError("Login request cannot be null.");
        return Results.BadRequest(new
        {
            message = "Request cannot be null"
        });
    }

    if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
    {
        logger.LogWarning("Email or password missing in /login request.");

        return Results.BadRequest(new
        {
            message = "Email and password are required."
        });
    }

    var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

    if (user == null)
    {
        logger.LogWarning("Invalid login attempt for email: {Email}", request.Email);

        return Results.BadRequest(new
        {
            message = "Invalid credentials."
        });
    }

    var hashedPassword = PasswordHash.HashPasswordWithSalt(request.Password, user.PasswordSalt);

    if (hashedPassword != user.HashedPassword)
    {
        logger.LogWarning("Invalid login attempt for email: {Email}", request.Email);

        return Results.BadRequest(new
        {
            message = "Invalid credentials."
        });
    }

    user.LastLoginAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();

    logger.LogInformation("User with email {Email} logged in successfully.", request.Email);

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

app.MapPost("/submitKycForm",  [Authorize] async (SubmitKycFormRequest request, AppDbContext dbContext, ILogger<Program> logger) =>
{
    logger.LogInformation("Received KYC form submission request for PhoneNumber: {PhoneNumber}", request.PhoneNumber);

    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(request);

    bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

    if (!isValid)
    {
        logger.LogWarning("Validation failed for KYC form submission for PhoneNumber: {PhoneNumber}. Errors: {ValidationErrors}", request.PhoneNumber, validationResults);

        return Results.BadRequest(validationResults);
    }

    if (request == null)
    {
        logger.LogError("Received a null request for KYC form submission");

        return Results.BadRequest(new
        {
            message = "Request cannot be null"
        });
    }

    var kycRequestPending = await dbContext.KycForms.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.KycStatus == KycStatus.Pending);

    if (kycRequestPending != null)
    {
        logger.LogWarning("KYC form for PhoneNumber: {PhoneNumber} is already pending.", request.PhoneNumber);

        return Results.BadRequest(new
        {
            message = "You currently have a pending KYC request, Our people are currently reviewing your information and you will get a response shortly."
        });
    }

    var kycRequestConfirmed = await dbContext.KycForms.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.KycStatus == KycStatus.Confirmed);

    if (kycRequestConfirmed != null)
    {
        logger.LogInformation("KYC form for PhoneNumber: {PhoneNumber} has already been confirmed.", request.PhoneNumber);

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

    logger.LogInformation("Successfully submitted KYC form for PhoneNumber: {PhoneNumber}. ID: {KycFormId}", request.PhoneNumber, kycForm.Id);


    return Results.Ok(new
    {
        message = "Kyc form submitted sucessfully, our team will review it and respond within 24 hours"
    });
});

app.MapPost("/confirmKycForm", [Authorize(Policy = "AdminOnly")] async (ConfirmKycFormRequest request, AppDbContext dbContext, ILogger<Program> logger) =>
{
    logger.LogInformation("Received KYC confirmation request for PhoneNumber: {PhoneNumber}", request.PhoneNumber);

    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(request);

    bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

    if (!isValid)
    {
        logger.LogWarning("Validation failed for KYC confirmation for PhoneNumber: {PhoneNumber}. Errors: {ValidationErrors}", request.PhoneNumber, validationResults);

        return Results.BadRequest(validationResults);
    }

    if (request == null)
    {
        logger.LogError("Received a null request for KYC confirmation");

        return Results.BadRequest(new
        {
            message = "Request cannot be null"
        });
    }

    var kycRequestApproved= await dbContext
                .KycForms.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.KycStatus == KycStatus.Confirmed);

    if (kycRequestApproved != null)
    {
        logger.LogWarning("KYC form for PhoneNumber: {PhoneNumber} has already been confirmed.", request.PhoneNumber);

        return Results.BadRequest(new
        {
            message = "This customer already has a confirmed KYC"
        });
    }


    logger.LogInformation("Checking for pending KYC form for PhoneNumber: {PhoneNumber}", request.PhoneNumber);


    var kycRequestToConfirm = await dbContext.KycForms
                    .SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.KycStatus == KycStatus.Pending);

    if (kycRequestToConfirm == null)
    {
        logger.LogWarning("No pending KYC form found for PhoneNumber: {PhoneNumber}.", request.PhoneNumber);

        return Results.BadRequest(new
        {
            message = "Kyc form does not exist."
        });
    }

    kycRequestToConfirm.KycStatus = KycStatus.Confirmed;

    logger.LogInformation("KYC form for PhoneNumber: {PhoneNumber} has been confirmed.", request.PhoneNumber);

    var userCredit = await dbContext.UserCredits
       .SingleOrDefaultAsync(uc => uc.PhoneNumber == request.PhoneNumber && uc.CreditStatus == CreditStatus.Credited);

    if (userCredit != null)
    {
        logger.LogWarning("Customer with PhoneNumber: {PhoneNumber} has already received the 200 Naira credit.", request.PhoneNumber);

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

    logger.LogInformation("200 Naira credit successfully added for PhoneNumber: {PhoneNumber}. Credit ID: {CreditId}", request.PhoneNumber, newCredit.Id);

    return Results.Ok(new
    { 
        message = "Kyc form confirmed and customer credited with 200 naira." 
    });

});

app.MapPost("/rejectKycForm", [Authorize(Policy = "AdminOnly")] async (RejectKycFormRequest request, AppDbContext dbContext, ILogger<Program> logger) =>
{
    logger.LogInformation("Received KYC rejection request for PhoneNumber: {PhoneNumber}", request.PhoneNumber);

    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(request);

    bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

    if (!isValid)
    {
        logger.LogWarning("Validation failed for KYC rejection for PhoneNumber: {PhoneNumber}. Errors: {ValidationErrors}", request.PhoneNumber, validationResults);

        return Results.BadRequest(validationResults);
    }

    if (request == null)
    {
        logger.LogError("Received a null request for KYC rejection");

        return Results.BadRequest(new
        {
            message = "Request cannot be null"
        });
    }

    logger.LogInformation("Checking if the KYC form for PhoneNumber: {PhoneNumber} exists and is in pending status.", request.PhoneNumber);

    // Check if the KYC form with the given phone number exists and is pending
    var kycRequestToReject = await dbContext.KycForms
                    .SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.KycStatus == KycStatus.Pending);

    if (kycRequestToReject == null)
    {
        logger.LogWarning("No pending KYC form found for PhoneNumber: {PhoneNumber}.", request.PhoneNumber);

        return Results.BadRequest(new
        {
            message = "Kyc form does not exist or is not in pending status."
        });
    }

    // Mark the KYC form as rejected
    kycRequestToReject.KycStatus = KycStatus.Rejected;

    logger.LogInformation("KYC form for PhoneNumber: {PhoneNumber} has been rejected.", request.PhoneNumber);

    await dbContext.SaveChangesAsync();

    logger.LogInformation("KYC rejection for PhoneNumber: {PhoneNumber} processed successfully.", request.PhoneNumber);

    return Results.Ok(new
    {
        message = "Kyc form has been rejected."
    });
});

#endregion


app.Run();

public partial class Program { }
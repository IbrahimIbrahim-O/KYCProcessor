using Dapper;
using KYCProcessor.Api.Dapper;
using KYCProcessor.Api.Helpers;
using KYCProcessor.Api.Interfaces;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Models;
using KYCProcessor.Data.Response;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace KYCProcessor.Api.Services
{
    public class LoginService : ILoginService
    {
        private readonly IDapperService _dapperService;
        private readonly ILogger<LoginService> _logger;
        private readonly IConfiguration _config;

        public LoginService(IDapperService dapperService, ILogger<LoginService> logger, IConfiguration config)
        {
            _dapperService = dapperService;
            _logger = logger;
            _config = config;
        }

        public async Task<LoginResponse> LoginAsync(LoginUserRequest request)
        {
            var response = new LoginResponse();

            try
            {
                if (request == null)
                {
                    _logger.LogError("Login request cannot be null.");
                    response.Success = false;
                    response.Message = "Request cannot be null.";
                    return response;
                }

                // Validate the request
                var validationResults = new List<ValidationResult>();
                var context = new ValidationContext(request);

                bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

                if (!isValid)
                {
                    _logger.LogWarning("Validation failed for /login request: {ValidationResults}", validationResults);
                    response.Success = false;
                    response.Message = "Validation failed.";
                    response.ValidationErrors = validationResults;
                    return response;
                }

                

                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    _logger.LogWarning("Email or password missing in /login request.");

                    response.Success = false;
                    response.Message = "Email and password are required.";
                    return response;
                }

                // Prepare parameters
                var param = new DynamicParameters();

                // Query to fetch user based on email
                var query = $"SELECT * FROM Users WHERE Email = '{request.Email}'";
                var user =  _dapperService.Get<User>(query, param, CommandType.Text);

                if (user == null)
                {
                    _logger.LogWarning("Invalid login attempt for email: {Email}", request.Email);

                    response.Success = false;
                    response.Message = "Invalid credentials.";
                    return response;
                }

                // Validate password
                var hashedPassword = PasswordHash.HashPasswordWithSalt(request.Password, user.PasswordSalt);

                if (hashedPassword != user.HashedPassword)
                {
                    _logger.LogWarning("Invalid login attempt for email: {Email}", request.Email);

                    response.Success = false;
                    response.Message = "Invalid credentials.";
                    return response;
                }

                user.LastLoginAt = DateTime.UtcNow;
                param = new DynamicParameters();

                // Update the LastLoginAt in the database
                var updateQuery = $"UPDATE Users SET LastLoginAt = '{user.LastLoginAt}' WHERE Id = '{user.Id}'";
                 _dapperService.Get<int>(updateQuery, param, CommandType.Text);

                _logger.LogInformation("User with email {Email} logged in successfully.", request.Email);

                // Generate JWT token
                var jwtToken = new JwtToken(_config);
                var token = jwtToken.GenerateJwtToken(user);

                var userInfo = new UserInfo
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Id = user.Id,
                    PhoneNumber = user.PhoneNumber
                };

                response.Success = true;
                response.Message = "Login successful";
                response.Token = token;
                response.UserInfo = userInfo;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the login process.");
                response.Success = false;
                response.Message = "An error occurred while processing your login request.";
                return response;
            }
        }
    }
}

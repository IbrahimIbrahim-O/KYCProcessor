using Dapper;
using KYCProcessor.Api.Dapper;
using KYCProcessor.Api.Helpers;
using KYCProcessor.Api.Interfaces;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Models;
using KYCProcessor.Data.Response;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace KYCProcessor.Api.Services
{
    public class SignUpService : ISignUpService
    {
        private readonly IDapperService _dapperService;
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        public SignUpService(IConfiguration configuration, IDapperService dapperService, ILogger<SignUpService> logger)
        {
            _dapperService = dapperService;
            _logger = logger;
            _config = configuration;
        }
        public async Task<SignUpResponse> HandleSignUp(SignUpRequest request)
        {
            var response = new SignUpResponse();
            try
            {

                _logger.LogInformation("Handling /signup request for email: {Email}", request.Email);

                var validationResults = new List<ValidationResult>();
                var context = new ValidationContext(request);

                bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

                if (!isValid)
                {
                    _logger.LogWarning("Validation failed for /signup request: {ValidationResults}", validationResults);
                    response.Success = false;
                    response.Message = "Validation failed";
                    response.ValidationErrors = validationResults;
                    return response;
                }

                if (request == null)
                {
                    _logger.LogError("Signup request cannot be null.");
                    response.Success = false;
                    response.Message = "Request cannot be null";
                    return response;
                }

                // Check for existing email
                var _param = new DynamicParameters();
                var existingUserQuery = $"SELECT * FROM Users WHERE Email = '{request.Email}'";
                _logger.LogInformation(existingUserQuery);

                var existingUser = _dapperService.Get<User>(existingUserQuery, _param, CommandType.Text);

                if (existingUser != null)
                {
                    _logger.LogWarning("User with email {Email} already exists.", request.Email);
                    response.Success = false;
                    response.Message = "User with this email already exists.";
                    return response;
                }

                // Check for existing phone number
                var existingPhoneQuery = $"SELECT * FROM Users WHERE PhoneNumber = '{request.PhoneNumber}'";
                _logger.LogInformation(existingPhoneQuery);

                var existingPhoneNumber = _dapperService.Get<User>(existingPhoneQuery, _param, CommandType.Text);

                if (existingPhoneNumber != null)
                {
                    _logger.LogWarning("User with phone number {PhoneNumber} already exists.", request.PhoneNumber);
                    response.Success = false;
                    response.Message = "User with this phone number already exists.";
                    return response;
                }

                // Hash the password
                var (hashedPassword, salt) = PasswordHash.HashPassword(request.Password);

                _param = new DynamicParameters();
                var newUserId = Guid.NewGuid();

                // Insert user
                var insertUserQuery = $"insert into users " +
                                      $"(id,email,firstname,lastname,gender,phoneNumber,hashedPassword, passwordSalt, createdAt)" +
                                      $"values ('{newUserId}','{request.Email}','{request.FirstName}', '{request.LastName}', '{(int)request.Gender}'," +
                                      $" '{request.PhoneNumber}', '{hashedPassword}', '{salt}', '{DateTime.Now}')";

                _logger.LogInformation(insertUserQuery);
                var result = _dapperService.Insert<int>(insertUserQuery, _param, CommandType.Text);

                // Confirm user creation
                var userConfirmQuery = $"select * from users where id ='{newUserId}'";
                var user = _dapperService.Get<User>(userConfirmQuery, _param, CommandType.Text);

                if (user == null)
                {
                    _logger.LogError("Failed to create user with email: {Email}.", request.Email);
                    response.Success = false;
                    response.Message = "User creation failed";
                    return response;
                }

                _logger.LogInformation("New user with email {Email} created successfully.", request.Email);

                var jwtToken = new JwtToken(_config);
                var token = jwtToken.GenerateJwtToken(user);

                response.Success = true;
                response.Message = "Signup successful";
                response.Token = token;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the signup process.");
                response.Success = false;
                response.Message = "An error occurred during signup.";
                return response;
            }
        }
    }
}

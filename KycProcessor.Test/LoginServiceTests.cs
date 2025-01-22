using Moq;
using KYCProcessor.Api.Dapper;
using KYCProcessor.Api.Interfaces;
using KYCProcessor.Api.Services;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Models;
using KYCProcessor.Data.Response;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Xunit;
using Dapper;
using KYCProcessor.Api.Helpers;
using System.ComponentModel.DataAnnotations;

namespace KYCProcessor.Tests
{
    public class LoginServiceTests
    {
        private readonly Mock<IDapperService> _mockDapperService;
        private readonly Mock<ILogger<LoginService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly LoginService _loginService;

        public LoginServiceTests()
        {
            _mockDapperService = new Mock<IDapperService>();
            _mockLogger = new Mock<ILogger<LoginService>>();
            _mockConfig = new Mock<IConfiguration>();
            _loginService = new LoginService(_mockDapperService.Object, _mockLogger.Object, _mockConfig.Object);
        }

        [Fact]
        public async Task LoginAsync_ReturnsSuccess_WhenCredentialsAreValid()
        {
            // Arrange
            var request = new LoginUserRequest
            {
                Email = "test@example.com",
                Password = "Password123"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                PasswordSalt = "salt",
                HashedPassword = PasswordHash.HashPasswordWithSalt(request.Password, "salt"), // Assuming the PasswordHash class hashes correctly
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "1234567890",
                LastLoginAt = DateTime.UtcNow
            };

            // Mock Dapper behavior
            var param = new DynamicParameters();
            _mockDapperService.Setup(d => d.Get<User>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
                .Returns(user);

            _mockDapperService.Setup(d => d.Get<int>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
                .Returns(1); // Assume the update query runs successfully

            // Act
            var response = await _loginService.LoginAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Login successful", response.Message);
            Assert.NotNull(response.Token); 
            Assert.Equal("John", response.UserInfo.FirstName);
            Assert.Equal("Doe", response.UserInfo.LastName);
            Assert.Equal("test@example.com", response.UserInfo.Email);
        }

        [Fact]
        public async Task LoginAsync_ReturnsFailure_WhenCredentialsAreInvalid()
        {
            // Arrange
            var request = new LoginUserRequest
            {
                Email = "test@example.com",
                Password = "WrongPassword123"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                PasswordSalt = "salt",
                HashedPassword = PasswordHash.HashPasswordWithSalt("Password123", "salt"), // Correct password is Password123
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "1234567890",
                LastLoginAt = DateTime.UtcNow
            };

            // Mock Dapper behavior
            var param = new DynamicParameters();
            _mockDapperService.Setup(d => d.Get<User>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
                .Returns(user);

            // Act
            var response = await _loginService.LoginAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Invalid credentials.", response.Message);
        }

        [Fact]
        public async Task LoginAsync_ReturnsFailure_WhenUserDoesNotExist()
        {
            // Arrange
            var request = new LoginUserRequest
            {
                Email = "nonexistent@example.com",
                Password = "Password123"
            };

            // Mock Dapper behavior to return null (no user found)
            var param = new DynamicParameters();
            _mockDapperService.Setup(d => d.Get<User>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
                .Returns((User)null); // No user found

            // Act
            var response = await _loginService.LoginAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Invalid credentials.", response.Message);
        }

        [Fact]
        public async Task LoginAsync_ReturnsFailure_WhenPasswordIsMissing()
        {
            // Arrange
            var request = new LoginUserRequest
            {
                Email = "test@example.com",
                Password = ""  // Empty password
            };

            // Act
            var response = await _loginService.LoginAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Validation failed.", response.Message);
        }

        [Fact]
        public async Task LoginAsync_ReturnsFailure_WhenEmailIsMissing()
        {
            // Arrange
            var request = new LoginUserRequest
            {
                Email = "",  // Empty email
                Password = "Password123"
            };

            // Act
            var response = await _loginService.LoginAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Validation failed.", response.Message);
        }

        [Fact]
        public async Task LoginAsync_ReturnsFailure_WhenRequestIsNull()
        {
            // Arrange
            LoginUserRequest request = null;

            // Act
            var response = await _loginService.LoginAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Request cannot be null.", response.Message);
        }

        [Fact]
        public async Task LoginAsync_ReturnsFailure_WhenValidationFails()
        {
            // Arrange
            var request = new LoginUserRequest
            {
                Email = "",  // Invalid email (empty)
                Password = ""  // Invalid password (empty)
            };

            // Mock the behavior of validation
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(request);
            bool isValid = Validator.TryValidateObject(request, context, validationResults, true);

            var response = new LoginResponse
            {
                Success = false,
                Message = "Validation failed.",
                ValidationErrors = validationResults
            };

            // Act
            var responseResult = await _loginService.LoginAsync(request);

            // Assert
            Assert.False(responseResult.Success);
            Assert.Equal("Validation failed.", responseResult.Message);
        }


    }
}

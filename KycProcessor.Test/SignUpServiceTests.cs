using Dapper;
using KYCProcessor.Api.Dapper;
using KYCProcessor.Api.Helpers;
using KYCProcessor.Api.Services;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Enums;
using KYCProcessor.Data.Models;
using KYCProcessor.Data.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Data;

namespace KycProcessor.Test
{
    public class SignUpServiceTests
    {
        private readonly Mock<IDapperService> _mockDapperService;
        private readonly Mock<ILogger<SignUpService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly SignUpService _signUpService;

        public SignUpServiceTests()
        {
            // Mock the dependencies
            _mockDapperService = new Mock<IDapperService>();
            _mockLogger = new Mock<ILogger<SignUpService>>();
            _mockConfig = new Mock<IConfiguration>();

            // Initialize the SignUpService with the mocked dependencies
            _signUpService = new SignUpService(_mockConfig.Object, _mockDapperService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task HandleSignUp_ReturnsOk_WhenSignUpIsSuccessful()
        {
            // Arrange
            var request = new SignUpRequest
            {
                Email = "newuser@example.com",
                PhoneNumber = "0987654321",
                Password = "Password123",
                FirstName = "Sam",
                LastName = "Brown",
                Gender = Gender.male
            };

           
            _mockDapperService
                .SetupSequence(d => d.Get<User>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
                .Returns((User)null)  // First call (email check)
                .Returns((User)null).Returns(new User { Email = "newuser@example.com" });

            // Mock insert result to simulate successful insertion
            _mockDapperService
                .Setup(d => d.Insert<int>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                .Returns(1); // Simulate successful insertion
         
            // Act
            var result = await _signUpService.HandleSignUp(request);

            // Ensure that a token is returned
            Assert.NotNull(result.Token);
            Assert.IsType<string>(result.Token.AccessToken);
        }

        [Fact]
        public async Task HandleSignUp_ReturnsBadRequest_WhenEmailAlreadyExists()
        {
            // Arrange
            var request = new SignUpRequest
            {
                Email = "existinguser@example.com",
                PhoneNumber = "0987654321",
                Password = "Password123",
                FirstName = "John",
                LastName = "Doe",
                Gender = Gender.male
            };

            // Mock DapperService to simulate an existing user for the email check
            _mockDapperService
                .SetupSequence(d => d.Get<User>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
                .Returns(new User { Email = "existinguser@example.com" })  // Email already exists
                .Returns((User)null); // Phone number doesn't exist

            // Act
            var result = await _signUpService.HandleSignUp(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User with this email already exists.", result.Message);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task HandleSignUp_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange: create an invalid request with missing mandatory fields (e.g., email)
            var request = new SignUpRequest
            {
                Email = "",
                PhoneNumber = "0987654321",
                Password = "Password123",
                FirstName = "Invalid",
                LastName = "User",
                Gender = Gender.male
            };

            // Mock DapperService to simulate no existing user
            _mockDapperService
                .SetupSequence(d => d.Get<User>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
                .Returns((User)null)
                .Returns((User)null);

            // Act
            var result = await _signUpService.HandleSignUp(request);

            // Assert: the result should indicate validation failure
            Assert.False(result.Success);
            Assert.Equal("Validation failed", result.Message);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task HandleSignUp_ReturnsServerError_WhenExceptionOccurs()
        {
            // Arrange
            var request = new SignUpRequest
            {
                Email = "newuser@example.com",
                PhoneNumber = "0987654321",
                Password = "Password123",
                FirstName = "Sam",
                LastName = "Brown",
                Gender = Gender.male
            };

            // Mock DapperService to throw an exception during the insertion
            _mockDapperService
                .SetupSequence(d => d.Get<User>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
                .Returns((User)null)
                .Returns((User)null);

            _mockDapperService
                .Setup(d => d.Insert<int>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                .Throws(new Exception("Database error"));

            // Act
            var result = await _signUpService.HandleSignUp(request);

            // Assert: the result should be a server error
            Assert.False(result.Success);
            Assert.Equal("An error occurred during signup.", result.Message);
            Assert.Null(result.Token);
        }


    }
}

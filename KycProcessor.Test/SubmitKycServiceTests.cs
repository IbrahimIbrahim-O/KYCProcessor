using Dapper;
using KYCProcessor.Api.Dapper;
using KYCProcessor.Api.Services;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Enums;
using KYCProcessor.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KycProcessor.Test
{
    public class SubmitKycServiceTests
    {
        private readonly Mock<IDapperService> _mockDapperService;
        private readonly Mock<ILogger<SubmitKycService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly SubmitKycService _submitKycService;


        public SubmitKycServiceTests()
        {
            // Mock the dependencies
            _mockDapperService = new Mock<IDapperService>();
            _mockLogger = new Mock<ILogger<SubmitKycService>>();
            _mockConfig = new Mock<IConfiguration>();

            _mockConfig.Setup(config => config["ConnectionStrings:DefaultConnection"])
                 .Returns("Server=SANUSI-M\\SQLEXPRESS;Database=KYCDb;Trusted_Connection=True;MultipleActiveResultSets=true;");

            // Initialize the SubmitKycService with the mocked dependencies
            _submitKycService = new SubmitKycService(_mockDapperService.Object, _mockLogger.Object, _mockConfig.Object);
        }

        [Fact]
        public async Task SubmitKycFormAsync_ReturnsSuccess_WhenValidRequest()
        {
            // Arrange
            var request = new SubmitKycFormRequest
            {
                PhoneNumber = "1234567890",
                FirstName = "John"
            };

            // Mock DapperService behavior to simulate no pending or confirmed KYC
            _mockDapperService
                .SetupSequence(d => d.Get<KycForm>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
                .Returns((KycForm)null)  // No confirmed KYC
                .Returns((KycForm)null);  // No pending KYC

            // Mock the insert query to return successful insertion
            _mockDapperService
                .Setup(d => d.Get<int>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                .Returns(1);  // Successful insertion

            // Act
            var result = await _submitKycService.SubmitKycFormAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("KYC form submitted successfully. Our team will review it and respond within 24 hours.", result.Message);
        }

        [Fact]
        public async Task SubmitKycFormAsync_ReturnsError_WhenValidationFails()
        {
            // Arrange
            var request = new SubmitKycFormRequest
            {
                PhoneNumber = ""  // Invalid phone number (empty)
            };

            // Act
            var result = await _submitKycService.SubmitKycFormAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Validation failed", result.Message);
            Assert.NotEmpty(result.ValidationErrors);
        }

        [Fact]
        public async Task SubmitKycFormAsync_ReturnsError_WhenPendingKycExists()
        {
            // Arrange
            var request = new SubmitKycFormRequest
            {
                PhoneNumber = "1234567890",
                FirstName = "John"
            };

            // Mock DapperService to simulate pending KYC
            _mockDapperService
                .Setup(d => d.Get<KycForm>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                .Returns(new KycForm { KycStatus = KycStatus.Pending });

            // Act
            var result = await _submitKycService.SubmitKycFormAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("You currently have a pending KYC request. Our team is reviewing your information and will respond shortly.", result.Message);
        }

        [Fact]
        public async Task SubmitKycFormAsync_ReturnsError_WhenConfirmedKycExists()
        {
            // Arrange
            var request = new SubmitKycFormRequest
            {
                PhoneNumber = "12345670",
                FirstName = "John"
            };

            // Mock DapperService to simulate confirmed KYC
            _mockDapperService
                .SetupSequence(d => d.Get<KycForm>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                .Returns((KycForm)null)
                .Returns(new KycForm { KycStatus = KycStatus.Confirmed });

            // Act
            var result = await _submitKycService.SubmitKycFormAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Your KYC form has already been confirmed.", result.Message);
        }

        [Fact]
        public async Task SubmitKycFormAsync_ReturnsError_WhenExceptionOccurs()
        {
            // Arrange
            var request = new SubmitKycFormRequest
            {
                PhoneNumber = "1234567890",
                FirstName = "John"
            };

            // Mock the database queries to simulate no pending or confirmed KYC
            _mockDapperService
                .SetupSequence(d => d.Get<KycForm>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                .Returns((KycForm)null)  // No confirmed KYC
                .Returns((KycForm)null);  // No pending KYC

            // Simulate an exception during the insert query
            _mockDapperService.Setup(d => d.Get<int>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                               .Throws(new Exception("Database error"));

            // Act
            var result = await _submitKycService.SubmitKycFormAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("An error occurred during KYC form submission.", result.Message);
        }

    }
}

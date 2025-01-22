using Dapper;
using KYCProcessor.Api.Dapper;
using KYCProcessor.Api.Services;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Enums;
using KYCProcessor.Data.Models;
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
    public class RejectKycServiceTests
    {
        private readonly Mock<IDapperService> _mockDapperService;
        private readonly Mock<ILogger<RejectKycService>> _mockLogger;
        private readonly RejectKycService _rejectKycService;

        public RejectKycServiceTests()
        {
            // Mock the dependencies
            _mockDapperService = new Mock<IDapperService>();
            _mockLogger = new Mock<ILogger<RejectKycService>>();

            // Initialize the RejectKycService with the mocked dependencies
            _rejectKycService = new RejectKycService(_mockDapperService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task RejectKycFormAsync_ReturnsSuccess_WhenValidRequest()
        {
            // Arrange
            var request = new RejectKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            // Mock DapperService to simulate no pending KYC
            _mockDapperService
                .Setup(d => d.Get<KycForm>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                .Returns(new KycForm { KycStatus = KycStatus.Pending });  // Simulate pending KYC form

            // Mock the update query to simulate successful rejection
            _mockDapperService
                .Setup(d => d.Get<int>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                .Returns(1);  // Simulate successful rejection update

            // Act
            var result = await _rejectKycService.RejectKycFormAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("KYC form has been rejected.", result.Message);
        }

        [Fact]
        public async Task RejectKycFormAsync_ReturnsError_WhenValidationFails()
        {
            // Arrange
            var request = new RejectKycFormRequest
            {
                PhoneNumber = ""  // Invalid phone number (empty)
            };

            // Act
            var result = await _rejectKycService.RejectKycFormAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Validation failed", result.Message);
            Assert.NotEmpty(result.ValidationErrors);
        }

        [Fact]
        public async Task RejectKycFormAsync_ReturnsError_WhenNoPendingKycExists()
        {
            // Arrange
            var request = new RejectKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            // Mock DapperService to simulate no pending KYC
            _mockDapperService
                .Setup(d => d.Get<KycForm>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                .Returns((KycForm)null);  // Simulate no pending KYC form

            // Act
            var result = await _rejectKycService.RejectKycFormAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("KYC form does not exist or is not in pending status.", result.Message);
        }

        [Fact]
        public async Task RejectKycFormAsync_ReturnsError_WhenExceptionOccurs()
        {
            // Arrange
            var request = new RejectKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            // Mock DapperService to simulate a database error
            _mockDapperService
                .Setup(d => d.Get<KycForm>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                .Throws(new Exception("Database error"));

            // Act
            var result = await _rejectKycService.RejectKycFormAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("An error occurred during KYC rejection.", result.Message);
        }
    }
}

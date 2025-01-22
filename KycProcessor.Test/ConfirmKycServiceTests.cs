using KYCProcessor.Api.Dapper;
using KYCProcessor.Api.Interfaces;
using KYCProcessor.Api.Services;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Enums;
using KYCProcessor.Data.Models;
using KYCProcessor.Data.Response;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.Data;
using Dapper;

namespace KycProcessor.Test
{
    public class ConfirmKycServiceTests
    {
        private readonly Mock<IDapperService> _mockDapperService;
        private readonly Mock<ILogger<ConfirmKycService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly ConfirmKycService _confirmKycService;

        public ConfirmKycServiceTests()
        {
            // Mock the dependencies
            _mockDapperService = new Mock<IDapperService>();
            _mockLogger = new Mock<ILogger<ConfirmKycService>>();
            _mockConfig = new Mock<IConfiguration>();

            _mockConfig.Setup(config => config["ConnectionStrings:DefaultConnection"])
                 .Returns("Server=SANUSI-M\\SQLEXPRESS;Database=KYCDb;Trusted_Connection=True;MultipleActiveResultSets=true;");


            // Initialize the ConfirmKycService with the mocked dependencies
            _confirmKycService = new ConfirmKycService(_mockDapperService.Object, _mockLogger.Object, _mockConfig.Object);
        }

        [Fact]
        public async Task ConfirmKycFormAsync_ReturnsSuccess_WhenKycIsConfirmedAndCreditIssued()
        {
            // Arrange
            var request = new ConfirmKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            // Mock DapperService behavior to simulate no confirmed KYC and a pending KYC form
            _mockDapperService
                .SetupSequence(d => d.Get<KycForm>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
                .Returns((KycForm)null)  // No confirmed KYC
                .Returns(new KycForm())
                .Returns(new KycForm());

            // Pending KYC found
            _mockDapperService
                  .SetupSequence(d => d.Get<UserCredit>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
                  .Returns((UserCredit)null); // No credit found

            // Mock the update query to return a successful update
            _mockDapperService
                .Setup(d => d.Get<int>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                .Returns(1);  // Successful update

            // Act
            var result = await _confirmKycService.ConfirmKycFormAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("KYC confirmed and 200 Naira credit issued", result.Message);
        }

        [Fact]
        public async Task ConfirmKycFormAsync_ReturnsError_WhenKycAlreadyConfirmed()
        {
            // Arrange
            var request = new ConfirmKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            // Mock DapperService to simulate confirmed KYC
            _mockDapperService.Setup(d => d.Get<KycForm>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                              .Returns(new KycForm { KycStatus = KycStatus.Confirmed });

            // Act
            var result = await _confirmKycService.ConfirmKycFormAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("This customer already has a confirmed KYC", result.Message);
        }

        [Fact]
        public async Task ConfirmKycFormAsync_ReturnsError_WhenNoPendingKycFound()
        {
            // Arrange
            var request = new ConfirmKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            // Mock DapperService to simulate no pending KYC
            _mockDapperService.Setup(d => d.Get<KycForm>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                              .Returns((KycForm)null);  // No pending KYC found

            // Act
            var result = await _confirmKycService.ConfirmKycFormAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("No pending KYC form found", result.Message);
        }

        [Fact]
        public async Task ConfirmKycFormAsync_ReturnsError_WhenCreditAlreadyIssued()
        {
            // Arrange
            var request = new ConfirmKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            // Mock DapperService to simulate pending KYC and user having received credit
            _mockDapperService.SetupSequence(d => d.Get<KycForm>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                              .Returns((KycForm)null)  // No confirmed KYC
                              .Returns(new KycForm())
                              .Returns(new KycForm());  // Pending KYC

            _mockDapperService.Setup(d =>
                       d.Get<UserCredit>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                      .Returns(new UserCredit { CreditStatus = CreditStatus.Credited }); 

            // Act
            var result = await _confirmKycService.ConfirmKycFormAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Customer has already received credit", result.Message);
        }

        [Fact]
        public async Task ConfirmKycFormAsync_ReturnsError_WhenExceptionOccurs()
        {
            // Arrange
            var request = new ConfirmKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            // Mock the database queries to simulate pending KYC form
            _mockDapperService.SetupSequence(d => d.Get<KycForm>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                              .Returns((KycForm)null)  // No confirmed KYC
                              .Returns(new KycForm());  // Pending KYC

            // Mock the credit check to simulate no credit found
            _mockDapperService.Setup(d => d.Get<UserCredit>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                              .Returns((UserCredit)null);

            // Simulate an exception during the KYC status update
            _mockDapperService.Setup(d => d.Get<int>(It.IsAny<string>(), It.IsAny<DynamicParameters>(), CommandType.Text))
                              .Throws(new Exception("Database error"));

            // Act
            var result = await _confirmKycService.ConfirmKycFormAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("An error occurred during KYC confirmation", result.Message);
        }

    }
}
using KYCProcessor.Api.Helpers;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Enums;
using KYCProcessor.Data.Models;
using KYCProcessor.Data;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace KycProcessor.Test
{
    public class RejectKycFormTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly IConfiguration _configuration;

        public RejectKycFormTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _configuration = new ConfigurationBuilder()
          .AddInMemoryCollection(new Dictionary<string, string>
          {
                 { "Jwt:ValidAudience", "KYCProcessor" },
                { "Jwt:ValidIssuer", "KYCProcessor" },
                { "Jwt:Secret", "secret kyc processor is having fun all the way" },
                { "Jwt:ExpDuration", "1440" },
                { "Jwt:RefreshExpDuration", "5" },
                { "Jwt:ClockSkew", "5" }
          })
          .Build();
        }

        [Fact]
        public async Task RejectKycForm_ValidRequest_ReturnsSuccess()
        {
            // Arrange: Create in-memory database and insert a pending KYC form
            var options = new DbContextOptionsBuilder<AppDbContext>()
                                .UseInMemoryDatabase("InMemoryDb2")
                                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.Database.EnsureDeleted();  // Ensure the database is reset before each test
                dbContext.Database.EnsureCreated();

                // Insert a pending KYC form
                var pendingKyc = new KycForm
                {
                    Name = "John Doe",
                    Id = Guid.NewGuid(),
                    PhoneNumber = "1234567890",
                    KycStatus = KycStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.KycForms.Add(pendingKyc);
                dbContext.SaveChanges();
            }

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services
                        .SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb2"));
                });
            }).CreateClient();

            // Set up JWT token (admin privileges)
            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateAdminJwtToken();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Arrange: Prepare the request payload
            var request = new RejectKycFormRequest
            {
                PhoneNumber = "1234567890",
            };

            // Act: Send POST request to reject the KYC form
            var response = await client.PostAsJsonAsync("/rejectKycForm", request);

            // Assert: Check if the response is OK and contains the success message
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Kyc form has been rejected.", responseContent);

            // Verify the KYC status in the database
            using (var dbContext = new AppDbContext(options))
            {
                var kycForm = dbContext.KycForms.SingleOrDefault(k => k.PhoneNumber == "1234567890");
                Assert.NotNull(kycForm);
                Assert.Equal(KycStatus.Rejected, kycForm.KycStatus);
            }
        }

        [Fact]
        public async Task RejectKycForm_NonPendingRequest_ReturnsBadRequest()
        {
            // Arrange: Create in-memory database and insert a confirmed KYC form
            var options = new DbContextOptionsBuilder<AppDbContext>()
                                .UseInMemoryDatabase("InMemoryDb2")
                                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.Database.EnsureDeleted();  // Ensure the database is reset before each test
                dbContext.Database.EnsureCreated();

                // Insert a confirmed KYC form
                var confirmedKyc = new KycForm
                {
                    Name = "Jane Doe",
                    Id = Guid.NewGuid(),
                    PhoneNumber = "0987654321",
                    KycStatus = KycStatus.Confirmed,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.KycForms.Add(confirmedKyc);
                dbContext.SaveChanges();
            }

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services
                        .SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb2"));
                });
            }).CreateClient();

            // Set up JWT token (admin privileges)
            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateAdminJwtToken();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Arrange: Prepare the request payload
            var request = new RejectKycFormRequest
            {
                PhoneNumber = "0987654321",
            };

            // Act: Send POST request to reject the KYC form
            var response = await client.PostAsJsonAsync("/rejectKycForm", request);

            // Assert: Check if the response is BadRequest and contains the error message
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Kyc form does not exist or is not in pending status.", responseContent);
        }

        [Fact]
        public async Task RejectKycForm_NonExistentRequest_ReturnsBadRequest()
        {
            // Arrange: Create in-memory database and ensure it's empty
            var options = new DbContextOptionsBuilder<AppDbContext>()
                                .UseInMemoryDatabase("InMemoryDb2")
                                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.Database.EnsureDeleted();  // Ensure the database is reset before each test
                dbContext.Database.EnsureCreated();
            }

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services
                        .SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb2"));
                });
            }).CreateClient();

            // Set up JWT token (admin privileges)
            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateAdminJwtToken();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Arrange: Prepare the request payload
            var request = new RejectKycFormRequest
            {
                PhoneNumber = "1234567890",  // Phone number not in the database
            };

            // Act: Send POST request to reject the KYC form
            var response = await client.PostAsJsonAsync("/rejectKycForm", request);

            // Assert: Check if the response is BadRequest and contains the error message
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Kyc form does not exist or is not in pending status.", responseContent);
        }

        [Fact]
        public async Task RejectKycForm_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange: Create in-memory database and insert a pending KYC form
            var options = new DbContextOptionsBuilder<AppDbContext>()
                                .UseInMemoryDatabase("InMemoryDb2")
                                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.Database.EnsureDeleted();  // Ensure the database is reset before each test
                dbContext.Database.EnsureCreated();

                // Insert a pending KYC form
                var pendingKyc = new KycForm
                {
                    Name = "John Doe",
                    Id = Guid.NewGuid(),
                    PhoneNumber = "1234567890",
                    KycStatus = KycStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.KycForms.Add(pendingKyc);
                dbContext.SaveChanges();
            }

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services
                        .SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb2"));
                });
            }).CreateClient();

            // Set up JWT token (admin privileges)
            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateAdminJwtToken();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Arrange: Prepare the request payload with missing phone number
            var request = new RejectKycFormRequest
            {
                PhoneNumber = "",  // Invalid request
            };

            // Act: Send POST request to reject the KYC form
            var response = await client.PostAsJsonAsync("/rejectKycForm", request);

            // Assert: Check if the response is BadRequest and contains validation errors
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("errorMessage", responseContent);  // Validation error
        }

    }
}

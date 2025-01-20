using KYCProcessor.Api.Helpers;
using KYCProcessor.Data.DTOS;
using KYCProcessor.Data;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using KYCProcessor.Data.Enums;
using KYCProcessor.Data.Models;

namespace KycProcessor.Test
{
    public class SubmitKycFormTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly IConfiguration _configuration;

        public SubmitKycFormTests(WebApplicationFactory<Program> factory)
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
        public async Task SubmitKycForm_ValidRequest_ReturnsSuccess()
        {
            // Arrange: Create in-memory database and mock data
            var options = new DbContextOptionsBuilder<AppDbContext>()
                                .UseInMemoryDatabase("InMemoryDb1")
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

                    // Replace the actual DB context with an in-memory one for the test
                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb1"));
                });
            }).CreateClient();

            // Set up JWT token (admin privileges)
            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateJwtToken();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Arrange: Prepare the request payload
            var request = new SubmitKycFormRequest
            {
                PhoneNumber = "1234567890",
                FirstName = "John",
            };

            // Act: Send POST request to submit KYC form
            var response = await client.PostAsJsonAsync("/submitKycForm", request);

            // Assert: Check if the response is OK and contains success message
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Kyc form submitted sucessfully, our team will review it and respond within 24 hours", responseContent);
        }

        [Fact]
        public async Task SubmitKycForm_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange: Create in-memory database and mock data
            var options = new DbContextOptionsBuilder<AppDbContext>()
                                .UseInMemoryDatabase("InMemoryDb1")
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

                    // Replace the actual DB context with an in-memory one for the test
                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb1"));
                });
            }).CreateClient();

            // Set up JWT token (admin privileges)
            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateJwtToken();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Arrange: Prepare the request payload
            var request = new SubmitKycFormRequest
            {
                PhoneNumber = "",
                FirstName = "John",
            };

            // Act: Send POST request to submit KYC form
            var response = await client.PostAsJsonAsync("/submitKycForm", request);

            // Assert: 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("errorMessage", responseContent);
        }

        [Fact]
        public async Task SubmitKycForm_PendingRequest_ReturnsBadRequest()
        {
            // Arrange: Create in-memory database and insert a pending KYC form
            var options = new DbContextOptionsBuilder<AppDbContext>()
                                .UseInMemoryDatabase("InMemoryDb1")
                                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();

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

                    // Replace the actual DB context with an in-memory one for the test
                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb1"));
                });
            }).CreateClient();

            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateJwtToken();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Arrange: Prepare the request payload
            var request = new SubmitKycFormRequest
            {
                PhoneNumber = "1234567890",
                FirstName = "John",
            };

            // Act: Send POST request to submit KYC form
            var response = await client.PostAsJsonAsync("/submitKycForm", request);

            // Assert: Check if the response is BadRequest and contains pending message
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("You currently have a pending KYC request, Our people are currently reviewing your information and you will get a response shortly.", responseContent);  // Checking if pending message appears
        }

        [Fact]
        public async Task SubmitKycForm_ConfirmedRequest_ReturnsBadRequest()
        {
            // Arrange: Create in-memory database and insert a pending KYC form
            var options = new DbContextOptionsBuilder<AppDbContext>()
                                .UseInMemoryDatabase("InMemoryDb1")
                                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();

                var pendingKyc = new KycForm
                {
                    Name = "John Doe",
                    Id = Guid.NewGuid(),
                    PhoneNumber = "1234567890",
                    KycStatus = KycStatus.Confirmed,
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

                    // Replace the actual DB context with an in-memory one for the test
                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb1"));
                });
            }).CreateClient();

            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateJwtToken();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Arrange: Prepare the request payload
            var request = new SubmitKycFormRequest
            {
                PhoneNumber = "1234567890",
                FirstName = "John",
            };

            // Act: Send POST request to submit KYC form
            var response = await client.PostAsJsonAsync("/submitKycForm", request);

            // Assert: Check if the response is BadRequest and contains pending message
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Your Kyc form has been confirmed.", responseContent);  // Checking if pending message appears
        }
    }
}

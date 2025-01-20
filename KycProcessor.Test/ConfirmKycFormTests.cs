using KYCProcessor.Data.DTOS;
using KYCProcessor.Data.Enums;
using KYCProcessor.Data.Models;
using KYCProcessor.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Moq;
using KYCProcessor.Api.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace KycProcessor.Test
{
    public class ConfirmKycFormTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly IConfiguration _configuration;

        public ConfirmKycFormTests(WebApplicationFactory<Program> factory)
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
        public async Task ConfirmKycForm_ShouldConfirmKycAndCreditUser_WhenValidRequest()
        {
            // Arrange: Create in-memory database and mock data
            var options = new DbContextOptionsBuilder<AppDbContext>()
                            .UseInMemoryDatabase("InMemoryDb")
                            .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.Database.EnsureDeleted();  // Ensure the database is reset
                dbContext.Database.EnsureCreated();

                // Insert test data for a pending KYC form and user credit
                var pendingKyc = new KycForm
                {
                    Name = "dka",
                    Id = Guid.NewGuid(),
                    PhoneNumber = "1234567890",
                    KycStatus = KycStatus.Pending
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
                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb"));
                });
            }).CreateClient();

            var request = new ConfirmKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateAdminJwtToken();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Act: Send POST request to confirm KYC
            var response = await client.PostAsJsonAsync("/confirmKycForm", request);

            // Assert: Check if the response is OK and user is credited
             response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Kyc form confirmed and customer credited with 200 naira", responseContent);

            // Verify that user credit was added to the database
            using (var dbContext = new AppDbContext(options))
            {
                var userCredit = dbContext.UserCredits
                                           .SingleOrDefault(uc => uc.PhoneNumber == request.PhoneNumber && uc.CreditStatus == CreditStatus.Credited);
                Assert.NotNull(userCredit);
                Assert.Equal(200, userCredit.Amount);
            }
        }

        [Fact]
        public async Task ConfirmKycForm_ShouldReturnBadRequest_WhenKycFormAlreadyConfirmed()
        {
            // Arrange: Create an in-memory database with a confirmed KYC form for the phone number
            var options = new DbContextOptionsBuilder<AppDbContext>()
                            .UseInMemoryDatabase(databaseName: "InMemoryDb")
                            .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.Database.EnsureDeleted();  // Ensure the database is reset
                dbContext.Database.EnsureCreated();

                var confirmedKyc = new KycForm
                {
                    Name = "dka",
                    Id = Guid.NewGuid(),
                    PhoneNumber = "1234567890",
                    KycStatus = KycStatus.Confirmed
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

                    // Replace the actual DB context with an in-memory one for the test
                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb"));
                });
            }).CreateClient();

            var request = new ConfirmKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateAdminJwtToken();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Act: Send POST request to confirm KYC
            var response = await client.PostAsJsonAsync("/confirmKycForm", request);

            // Assert: Check if the response is BadRequest
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("This customer already has a confirmed KYC", responseContent);
        }

        [Fact]
        public async Task ConfirmKycForm_ShouldReturnBadRequest_WhenKycFormDoesNotExist()
        {
            // Arrange: Create an in-memory database with no pending KYC forms for the phone number
            var options = new DbContextOptionsBuilder<AppDbContext>()
                            .UseInMemoryDatabase(databaseName: "InMemoryDb")
                            .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.Database.EnsureDeleted();  // Ensure the database is reset
                dbContext.Database.EnsureCreated();
                // Insert no KYC forms
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
                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb"));
                });
            }).CreateClient();

            var request = new ConfirmKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateAdminJwtToken();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Act: Send POST request to confirm KYC
            var response = await client.PostAsJsonAsync("/confirmKycForm", request);

            // Assert: Check if the response is BadRequest
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Kyc form does not exist.", responseContent);
        }

        [Fact]
        public async Task ConfirmKycForm_ShouldReturnBadRequest_WhenUserAlreadyCredited()
        {
            // Arrange: Create an in-memory database with a pending KYC form and a credited user
            var options = new DbContextOptionsBuilder<AppDbContext>()
                            .UseInMemoryDatabase(databaseName: "InMemoryDb")
                            .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.Database.EnsureDeleted();  // Ensure the database is reset
                dbContext.Database.EnsureCreated();

                var pendingKyc = new KycForm
                {
                    Name = "dka",
                    Id = Guid.NewGuid(),
                    PhoneNumber = "1234567890",
                    KycStatus = KycStatus.Pending
                };

                dbContext.KycForms.Add(pendingKyc);

                var userCredit = new UserCredit
                {
                    Id = Guid.NewGuid(),
                    PhoneNumber = "1234567890",
                    Amount = 200,
                    CreditStatus = CreditStatus.Credited,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.UserCredits.Add(userCredit);
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
                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb"));
                });
            }).CreateClient();


            var request = new ConfirmKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateAdminJwtToken();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Act: Send POST request to confirm KYC
            var response = await client.PostAsJsonAsync("/confirmKycForm", request);

            // Assert: Check if the response is BadRequest
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("This customer has already received the 200 Naira credit.", responseContent);
        }

        [Fact]
        public async Task ConfirmKycForm_ShouldReturnForbidden_WhenUserIsNotAdmin()
        {
            // Arrange: Create a client with a non-admin JWT token
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
                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb"));
                });
            }).CreateClient();


            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateJwtToken();  // Simulating a non-admin user

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            var request = new ConfirmKycFormRequest
            {
                PhoneNumber = "1234567890"
            };

            // Act: Send POST request to confirm KYC
            var response = await client.PostAsJsonAsync("/confirmKycForm", request);

            // Assert: Check if the response is Unauthorized
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmKycForm_ShouldReturnBadRequest_WhenValidationFails()
        {
            // Arrange: Create an invalid request with an incorrect phone number
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
                    services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMemoryDb"));
                });
            }).CreateClient();


            var request = new ConfirmKycFormRequest
            {
                PhoneNumber = "invalid-phone-number" // Invalid phone number
            };

            var jwtToken = new JwtToken(_configuration);
            var token = jwtToken.GenerateAdminJwtToken();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            // Act: Send POST request with invalid data
            var response = await client.PostAsJsonAsync("/confirmKycForm", request);

            // Assert: Check if the response is BadRequest
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Invalid phone number format", responseContent);
        }



    }
}

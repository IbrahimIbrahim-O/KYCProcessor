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

namespace KycProcessor.Test
{
    public class KycFormTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly IConfiguration _configuration;

        public KycFormTests(WebApplicationFactory<Program> factory)
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
                            .UseInMemoryDatabase(databaseName: "InMemoryDb")
                            .Options;

            using (var dbContext = new AppDbContext(options))
            {
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


            //var client = _factory.CreateClient();
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
    }
}

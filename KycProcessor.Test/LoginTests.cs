using KYCProcessor.Data;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KycProcessor.Test
{
    public class LoginTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly Random _random;
        private readonly IConfiguration _configuration;

        public LoginTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _random = new Random();
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

        private string GenerateRandomEmail()
        {
            return $"testuser{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";
        }

        private string GenerateRandomPhoneNumber()
        {
            return "123456" + _random.Next(100000, 999999).ToString();
        }

        private void SetUpInMemoryDatabase()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                            .UseInMemoryDatabase("InMemoryDb3")
                            .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.Database.EnsureDeleted();  // Ensure the database is reset before each test
                dbContext.Database.EnsureCreated();
            }
        }

        private async Task<string> CreateNewUser(string email, string password)
        {
            var signUpRequest = new
            {
                Email = email,
                FirstName = "John",
                LastName = "Doe",
                Password = password,
                PhoneNumber = GenerateRandomPhoneNumber(),
                Gender = 0
            };

            var content = new StringContent(JsonConvert.SerializeObject(signUpRequest), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/signUp", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<dynamic>(responseString);

            return tokenResponse.Token;
        }

        [Fact]
        public async Task Test_Login_ValidCredentials_ReturnsOkAndToken()
        {
            // Arrange: Set up the database and create a user
            var email = GenerateRandomEmail();
            var password = "TestPassword123!";
            var token = await CreateNewUser(email, password);  // Create a new user to log in with

            var loginRequest = new
            {
                Email = email,
                Password = password
            };

            var content = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");

            // Act: Send login request to /login endpoint
            var response = await _client.PostAsync("/login", content);

            // Assert: Verify the response is successful and contains the token
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var responseContent = JsonConvert.DeserializeObject<dynamic>(responseString);

            Assert.NotNull(responseContent.Token);
            Assert.NotNull(responseContent.UserInfo);
            Assert.Equal(email, responseContent.UserInfo.Email.ToString());
        }


    }
}

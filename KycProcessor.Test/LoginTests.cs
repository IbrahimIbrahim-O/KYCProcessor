using KYCProcessor.Api.Helpers;
using KYCProcessor.Data.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KycProcessor.Test
{
    public class LoginTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly Random _random;

        public LoginTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _random = new Random();
        }

        private string GenerateRandomEmail()
        {
            return $"testuser{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";
        }

        private string GenerateRandomPhoneNumber()
        {
            return "123456" + _random.Next(100000, 999999).ToString();
        }

        [Fact]
        public async Task Test_Login_ValidCredentials_ReturnsOkAndToken()
        {
            // Arrange: Create a user via SignUp (this should be done in setup, but for simplicity we're doing it here)
            var signUpRequest = new
            {
                Email = GenerateRandomEmail(),
                FirstName = "John",
                LastName = "Doe",
                Password = "password",
                PhoneNumber = GenerateRandomPhoneNumber(),
                Gender = 0
            };

            var content = new StringContent(JsonConvert.SerializeObject(signUpRequest), Encoding.UTF8, "application/json");
            var signUpResponse = await _client.PostAsync("/signUp", content);
            var signUpResponseString = await signUpResponse.Content.ReadAsStringAsync();
            Assert.True(signUpResponse.IsSuccessStatusCode);

            // Now login with the same credentials
            var loginRequest = new
            {
                Email = signUpRequest.Email,
                Password = signUpRequest.Password
            };

            var loginContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");

            // Act: Make a POST request to /login
            var loginResponse = await _client.PostAsync("/login", loginContent);

            // Assert: The login response should contain the Token
            var loginResponseString = await loginResponse.Content.ReadAsStringAsync();

            Assert.True(loginResponse.IsSuccessStatusCode);

            var tokenResponse = JsonConvert.DeserializeObject<TokenResponseWrapper>(loginResponseString);

            Assert.NotNull(tokenResponse);
            Assert.NotNull(tokenResponse.Token);
            Assert.NotNull(tokenResponse.Token.AccessToken);
        }

        [Fact]
        public async Task Test_Login_InvalidPassword_ReturnsBadRequest()
        {
            // Arrange: Create a valid sign-up request
            var signUpRequest = new
            {
                Email = GenerateRandomEmail(),
                FirstName = "John",
                LastName = "Doe",
                Password = "rightPassword123!",
                PhoneNumber = GenerateRandomPhoneNumber(),
                Gender = 0
            };

            var content = new StringContent(JsonConvert.SerializeObject(signUpRequest), Encoding.UTF8, "application/json");
            var signUpResponse = await _client.PostAsync("/signUp", content);
            var signUpResponseString = await signUpResponse.Content.ReadAsStringAsync();
            Assert.True(signUpResponse.IsSuccessStatusCode);

            // Now login with an incorrect password
            var loginRequest = new
            {
                Email = signUpRequest.Email,
                Password = "WrongPassword"  // Incorrect password
            };

            var loginContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");

            // Act: Make a POST request to /login
            var loginResponse = await _client.PostAsync("/login", loginContent);

            // Assert: The login should fail with a BadRequest (400)
            Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
            var loginResponseString = await loginResponse.Content.ReadAsStringAsync();
            Assert.Contains("Invalid credentials.", loginResponseString);
        }

        [Fact]
        public async Task Test_Login_NonExistingEmail_ReturnsBadRequest()
        {
            // Arrange: Use an email that doesn't exist in the database
            var loginRequest = new
            {
                Email = GenerateRandomEmail(),
                Password = "SomePassword123!"
            };

            var loginContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");

            // Act: Make a POST request to /login
            var loginResponse = await _client.PostAsync("/login", loginContent);

            // Assert: The login should fail with a BadRequest (400)
            Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
            var loginResponseString = await loginResponse.Content.ReadAsStringAsync();
            Assert.Contains("Invalid credentials.", loginResponseString);
        }

    }
}
using KYCProcessor.Api.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;
using System.Text.Json;

namespace KycProcessor.Test
{
    public class SignUpTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly Random _random;

        public SignUpTests(WebApplicationFactory<Program> factory)
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
        public async Task Test_SignUp_ValidRequest_ReturnsOkAndToken()
        {
            // Arrange
            var signUpRequest = new
            {
                Email = GenerateRandomEmail(),
                FirstName = "John",
                LastName = "Dofe",
                Password = "TestPassword123!",
                PhoneNumber = GenerateRandomPhoneNumber(),
                Gender = 0
            };

            var content = new StringContent(JsonConvert.SerializeObject(signUpRequest), System.Text.Encoding.UTF8, "application/json");

            // Act: Send the sign-up POST request to the "/signup" endpoint
            var response = await _client.PostAsync("/signUp", content);

            // Assert: Verify the response is successful 
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            // Deserialize the response into the TokenResponse object
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponseWrapper>(responseString);

            // Assert: Check that the response contains the AccessToken, CreatedAt, and AccessTokenExp
            Assert.NotNull(tokenResponse);
            Assert.NotNull(tokenResponse.Token.AccessToken);
        }

        [Fact]
        public async Task Test_SignUp_ExistingEmail_ReturnsBadRequest()
        {
            // Arrange: Create a valid sign-up request with an existing email
            var signUpRequest = new
            {
                Email = GenerateRandomEmail(),
                FirstName = "Jane",
                LastName = "Doe",
                Password = "TestPassword123!",
                PhoneNumber = GenerateRandomPhoneNumber(),
                Gender = 1  
            };

            var content = new StringContent(JsonConvert.SerializeObject(signUpRequest), Encoding.UTF8, "application/json");

            // Act: Make the first POST request to /signUp (this should succeed)
            var firstResponse = await _client.PostAsync("/signUp", content);
            firstResponse.EnsureSuccessStatusCode();

            // Act: Make the second POST request with the same email (this should fail)
            var secondResponse = await _client.PostAsync("/signUp", content);

            // Assert: Ensure the second request fails with a BadRequest
            Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

            var responseString = await secondResponse.Content.ReadAsStringAsync();

            Assert.Contains("User with this email already exists.", responseString);
        }


        [Fact]
        public async Task Test_SignUp_ExistingPhoneNumber_ReturnsBadRequest()
        {
            // Arrange: Create a valid sign-up request with an existing email
            var signUpRequest = new
            {
                Email = GenerateRandomEmail(),
                FirstName = "Jane",
                LastName = "Doe",
                Password = "TestPassword123!",
                PhoneNumber = GenerateRandomPhoneNumber(),
                Gender = 1
            };

            var content = new StringContent(JsonConvert.SerializeObject(signUpRequest), Encoding.UTF8, "application/json");

            // Act: Make the first POST request to /signUp (this should succeed)
            var firstResponse = await _client.PostAsync("/signUp", content);
            firstResponse.EnsureSuccessStatusCode();

            var signUpRequest2 = new
            {
                Email = GenerateRandomEmail(),
                FirstName = "Jane",
                LastName = "Doe",
                Password = "password",
                PhoneNumber = signUpRequest.PhoneNumber,
                Gender = 1
            };

            var content2 = new StringContent(JsonConvert.SerializeObject(signUpRequest2), Encoding.UTF8, "application/json");

            // Act: Make the second POST request with the same email (this should fail)
            var secondResponse = await _client.PostAsync("/signUp", content2);

            // Assert: Ensure the second request fails with a BadRequest
            Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

            var responseString = await secondResponse.Content.ReadAsStringAsync();

            Assert.Contains("User with this phone number already exists.", responseString);
        }
    }
}
﻿using KYCProcessor.Api.Helpers;
using KYCProcessor.Data;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KycProcessor.Test
{
    public class SignUpTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly Random _random;
        private readonly IConfiguration _configuration;

        public SignUpTests(WebApplicationFactory<Program> factory)
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

        [Fact]
        public async Task Test_SignUp_ValidRequest_ReturnsOkAndToken()
        {
            // Arrange: Set up the in-memory database
            SetUpInMemoryDatabase();

            var signUpRequest = new
            {
                Email = GenerateRandomEmail(),
                FirstName = "John",
                LastName = "Dofe",
                Password = "TestPassword123!",
                PhoneNumber = GenerateRandomPhoneNumber(),
                Gender = 0
            };

            var content = new StringContent(JsonConvert.SerializeObject(signUpRequest), Encoding.UTF8, "application/json");

            // Act: Send the sign-up POST request to the "/signUp" endpoint
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
            // Arrange: Set up the in-memory database and create a valid sign-up request with an existing email
            SetUpInMemoryDatabase();

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
            // Arrange: Set up the in-memory database and create a valid sign-up request with an existing phone number
            SetUpInMemoryDatabase();

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

            // Act: Make the second POST request with the same phone number (this should fail)
            var secondResponse = await _client.PostAsync("/signUp", content2);

            // Assert: Ensure the second request fails with a BadRequest
            Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

            var responseString = await secondResponse.Content.ReadAsStringAsync();
            Assert.Contains("User with this phone number already exists.", responseString);
        }

        [Fact]
        public async Task ValidateSignUp_InvalidRequest_ReturnsBadRequestWithErrors()
        {
            // Invalid sign-up request with multiple validation issues (invalid email, invalid phone number, missing fields)
            var invalidRequest = new
            {
                Email = "invalid-email",       
                PhoneNumber = "12345",         
                FirstName = "",               
                LastName = "Doe",              
                Password = "Test",             
                Gender = 0
            };

            var content = new StringContent(JsonConvert.SerializeObject(invalidRequest), System.Text.Encoding.UTF8, "application/json");

            // Act: Send POST request to validate the sign-up request
            var response = await _client.PostAsync("/signUp", content);

            // Assert: Verify that the response is a BadRequest (400)
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseContent = JsonConvert.DeserializeObject<dynamic>(responseString);

            Assert.NotEmpty(responseContent);
        }
    }
}
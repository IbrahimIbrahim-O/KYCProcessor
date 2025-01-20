using KYCProcessor.Data.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KYCProcessor.Api.Helpers
{
    public class JwtToken
    {
        private readonly IConfiguration _config;
        public JwtToken(IConfiguration configuration)
        {
            _config = configuration;
        }
        public TokenResponse GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("firstname", user.FirstName),
                new Claim("id", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpDuration"])),
                signingCredentials: creds);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new TokenResponse
            {
                AccessToken = accessToken,
                AccessTokenExp = token.ValidTo,
                CreatedAt = DateTime.Now
            };
        }

        public TokenResponse GenerateJwtToken()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "testUser")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpDuration"])),
                signingCredentials: creds);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new TokenResponse
            {
                AccessToken = accessToken,
                AccessTokenExp = token.ValidTo,
                CreatedAt = DateTime.Now
            };
        }

        public TokenResponse GenerateAdminJwtToken()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "admin")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpDuration"])),
                signingCredentials: creds);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new TokenResponse
            {
                AccessToken = accessToken,
                AccessTokenExp = token.ValidTo,
                CreatedAt = DateTime.Now
            };
        }
    }

    public class TokenResponse
    {
        public string? AccessToken { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime AccessTokenExp { set; get; }
    }

    public class TokenResponseWrapper
    {
        public TokenResponse? Token { get; set; }
    }
}

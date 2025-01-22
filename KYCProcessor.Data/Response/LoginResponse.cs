using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCProcessor.Data.Response
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public TokenResponse? Token { get; set; }
        public UserInfo? UserInfo { get; set; }
        public List<ValidationResult> ValidationErrors { get; set; } = new List<ValidationResult>();
    }

    public class UserInfo
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public Guid Id { get; set; }
        public string? PhoneNumber { get; set; }
    }
}

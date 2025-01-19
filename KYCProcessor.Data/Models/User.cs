using KYCProcessor.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KYCProcessor.Data.Models
{
    public class User : BaseEntity
    {
        public string? Email { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public Gender Gender { get; set; }

        public string? PhoneNumber { get; set; }

        public string? HashedPassword { get; set; }

        public string? PasswordSalt { get; set; }
        public string Role { get; set; } = "user";
    }
}

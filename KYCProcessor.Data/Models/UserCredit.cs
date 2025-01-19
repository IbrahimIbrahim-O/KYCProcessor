using KYCProcessor.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCProcessor.Data.Models
{
    public class UserCredit : BaseEntity
    {
        public Guid Id { get; set; }

        public string? PhoneNumber { get; set; }

        public decimal Amount { get; set; }

        public CreditStatus CreditStatus { get; set; }

    }

   
}

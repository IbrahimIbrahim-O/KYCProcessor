using KYCProcessor.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCProcessor.Data.Models
{
    public class KycForm : BaseEntity
    {
        public string? Name { get; set; }

        public string? PhoneNumber { get; set; }

        public KycStatus KycStatus { get; set; } = KycStatus.Pending;
    }
}

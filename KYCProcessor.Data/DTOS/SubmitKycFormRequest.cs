using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCProcessor.Data.DTOS
{
    public class SubmitKycFormRequest
    {
        public string? FirstName { get; set; }

        public string? PhoneNumber { get; set; }
    }
}

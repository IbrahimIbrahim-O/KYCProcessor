using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCProcessor.Data.Response
{
    public class SubmitKycResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<ValidationResult>? ValidationErrors { get; set; }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCProcessor.Data.DTOS
{
    public class ValidationError
    {
        [JsonProperty("memberNames")]
        public List<string>? MemberNames { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }
    }
}

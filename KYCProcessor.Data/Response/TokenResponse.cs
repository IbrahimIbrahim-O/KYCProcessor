using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCProcessor.Data.Response
{
    public class TokenResponse
    {
        public string? AccessToken { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime AccessTokenExp { set; get; }
    }
}

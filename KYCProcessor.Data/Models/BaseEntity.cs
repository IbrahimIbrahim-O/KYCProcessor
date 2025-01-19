using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCProcessor.Data.Models
{
    public class BaseEntity
    {
        public Guid Id { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public Guid? CreatedBy { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastModifiedAt { get; set; }

        public Guid? ModifiedBy { get; set; }

    }
}

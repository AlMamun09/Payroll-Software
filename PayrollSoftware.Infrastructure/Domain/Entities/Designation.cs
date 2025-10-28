using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Domain.Entities
{
    public class Designation
    {
        [Key]
        public Guid DesignationId { get; set; }
        public string? DesignationName { get; set; }
        public bool IsActive { get; set; }
    }
}

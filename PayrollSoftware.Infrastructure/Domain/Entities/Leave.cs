using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Domain.Entities
{
    public class Leave
    {
        [Key]
        public Guid LeaveId { get; set; }
        public Guid EmployeeId { get; set; }
        public string? LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? LeaveStatus { get; set; }
        // Add this Status property
        public string? Status { get; set; } = "Pending";
        public string? Remarks { get; set; }
        // Navigation Property
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        // Calculated property for total days
        [NotMapped]
        public int TotalDays => (EndDate - StartDate).Days + 1;
    }
}
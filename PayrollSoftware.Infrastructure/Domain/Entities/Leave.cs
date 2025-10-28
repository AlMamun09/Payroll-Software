using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public int TotalDays { get; set; }
        public string? LeaveStatus { get; set; }
        public string? Remarks { get; set; }
    }
}

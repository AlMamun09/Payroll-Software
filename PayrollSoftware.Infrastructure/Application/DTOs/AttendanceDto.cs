using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class AttendanceDto
    {
        public Guid AttendanceId { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public TimeSpan? InTime { get; set; }
        public TimeSpan? OutTime { get; set; }
        public string? Status { get; set; }
        public decimal WorkingHours { get; set; }
        public TimeSpan? LateEntry { get; set; }      // optional
        public TimeSpan? EarlyLeave { get; set; }    // optional
    }
}
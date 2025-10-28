using System;

namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class LeaveDto
    {
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

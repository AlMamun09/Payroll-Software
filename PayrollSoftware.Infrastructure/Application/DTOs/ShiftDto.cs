using System;

namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class ShiftDto
    {
        public Guid ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsActive { get; set; }
    }
}

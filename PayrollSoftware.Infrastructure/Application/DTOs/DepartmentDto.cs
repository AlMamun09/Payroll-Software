using System;

namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class DepartmentDto
    {
        public Guid DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
    }
}

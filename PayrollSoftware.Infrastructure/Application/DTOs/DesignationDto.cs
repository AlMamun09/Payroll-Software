using System;

namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class DesignationDto
    {
        public Guid DesignationId { get; set; }
        public string? DesignationName { get; set; }
        public bool IsActive { get; set; }
    }
}

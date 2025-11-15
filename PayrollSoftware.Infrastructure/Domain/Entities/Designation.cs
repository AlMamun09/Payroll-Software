using System.ComponentModel.DataAnnotations;

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

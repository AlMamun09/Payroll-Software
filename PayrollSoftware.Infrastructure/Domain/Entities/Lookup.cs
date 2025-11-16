namespace PayrollSoftware.Infrastructure.Domain.Entities
{
    public class Lookup
    {
        public Guid LookupId { get; set; }
        public string LookupType { get; set; } = string.Empty;
        public string LookupValue { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}

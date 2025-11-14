namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class LookupDto
    {
        public Guid LookupId { get; set; }
        public string LookupType { get; set; } = string.Empty;
        public string LookupValue { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

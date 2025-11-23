using System.ComponentModel.DataAnnotations;

namespace PayrollSoftware.Infrastructure.Domain.Entities
{
    public class AttendanceImportFile
    {
        [Key]
        public Guid ImportId { get; set; }
        public string? FileName { get; set; }
        public byte[]? FileContent { get; set; }
        public string? Status { get; set; }
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public string? ErrorLog { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}

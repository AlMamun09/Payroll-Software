using Microsoft.AspNetCore.Http;

namespace PayrollSoftware.Infrastructure.Application.DTOs
{
    public class AttendanceImportDto
    {
        public IFormFile? ExcelFile { get; set; }
    }
}

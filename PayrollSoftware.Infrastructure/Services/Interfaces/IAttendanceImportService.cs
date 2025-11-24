using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Services.Interfaces
{
    public interface IAttendanceImportService
    {
        Task<(bool Success, Guid? ImportId, string Message)> UploadImportAsync(AttendanceImportDto model);
        Task<(string Status, int Percentage, int Processed, int Total, string? Errors)> CheckProgressAsync(Guid importId);
    }
}

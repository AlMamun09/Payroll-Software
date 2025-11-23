using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Infrastructure.Application.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<List<Attendance>> GetAllAttendencesAsync();
        Task<Attendance?> GetAttendanceByIdAsync(Guid attendanceId);
        Task AddAttendanceAsync(Attendance attendance);
        Task UpdateAttendanceAsync(Attendance attendance);
        Task<Guid> SaveImportFileAsync(AttendanceImportFile importFile);
        Task UpdateImportProgressAsync(
            Guid importId,
            int processed,
            int total,
            string status,
            string? errors = null
        );
        Task<AttendanceImportFile?> GetImportStatusAsync(Guid importId);
    }
}

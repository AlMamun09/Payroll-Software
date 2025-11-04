using PayrollSoftware.Infrastructure.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Application.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<List<Attendance>> GetAllAttendanceAsync();
        Task<List<Attendance>> GetAttendanceByEmployeeIdAsync(Guid employeeId);
        Task<Attendance?> GetAttendanceByIdAsync(Guid attendanceId);
        Task<Attendance> RecordAttendanceAsync(Attendance attendance);
        Task<Attendance> UpdateAttendanceAsync(Attendance attendance);
        Task DeleteAttendanceAsync(Guid attendanceId);
        Task<bool> HasAttendanceForDateAsync(Guid employeeId, DateTime date, Guid? excludeAttendanceId = null);
    }
}
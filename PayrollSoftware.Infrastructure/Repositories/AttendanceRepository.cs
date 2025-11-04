using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Repositories
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly ApplicationDbContext _context;

        public AttendanceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Attendance>> GetAllAttendanceAsync()
        {
            return await _context.Attendances
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();
        }

        public async Task<List<Attendance>> GetAttendanceByEmployeeIdAsync(Guid employeeId)
        {
            return await _context.Attendances
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();
        }

        public async Task<Attendance?> GetAttendanceByIdAsync(Guid attendanceId)
        {
            return await _context.Attendances.FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);
        }

        public async Task<Attendance> RecordAttendanceAsync(Attendance attendance)
        {
            await ValidateAttendanceAsync(attendance);

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return attendance;
        }

        public async Task<Attendance> UpdateAttendanceAsync(Attendance attendance)
        {
            await ValidateAttendanceAsync(attendance, isUpdate: true);

            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();

            return attendance;
        }

        public async Task DeleteAttendanceAsync(Guid attendanceId)
        {
            var attendance = await GetAttendanceByIdAsync(attendanceId);
            if (attendance == null)
                throw new ArgumentException("Attendance record not found.");

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasAttendanceForDateAsync(Guid employeeId, DateTime date, Guid? excludeAttendanceId = null)
        {
            var query = _context.Attendances
                .Where(a => a.EmployeeId == employeeId && a.AttendanceDate.Date == date.Date);

            if (excludeAttendanceId.HasValue)
                query = query.Where(a => a.AttendanceId != excludeAttendanceId.Value);

            return await query.AnyAsync();
        }

        #region Private Methods

        private async Task ValidateAttendanceAsync(Attendance attendance, bool isUpdate = false)
        {
            if (attendance.EmployeeId == Guid.Empty)
                throw new ArgumentException("EmployeeId is required.");

            //var employee = await _context.Employees.FindAsync(attendance.EmployeeId);
            //if (employee == null || !string.Equals(employee.Status, "Currently Active", StringComparison.OrdinalIgnoreCase))
            //    throw new InvalidOperationException("Attendance cannot be recorded for inactive or resigned employees.");

            if (attendance.InTime.HasValue && attendance.OutTime.HasValue && attendance.OutTime < attendance.InTime)
                throw new ArgumentException("OutTime cannot be before InTime.");

            if (!isUpdate && await HasAttendanceForDateAsync(attendance.EmployeeId, attendance.AttendanceDate))
                throw new InvalidOperationException("Attendance already recorded for this date.");

            // Auto-calculate WorkingHours
            if (attendance.InTime.HasValue && attendance.OutTime.HasValue)
            {
                attendance.WorkingHours = (decimal)(attendance.OutTime.Value - attendance.InTime.Value).TotalHours;
                attendance.Status = "Present";
            }
            else if (!attendance.InTime.HasValue && !attendance.OutTime.HasValue)
            {
                attendance.Status = "Absent";
            }

            // You can calculate LateEntry / EarlyLeave here if shift info available
        }

        #endregion
    }
}

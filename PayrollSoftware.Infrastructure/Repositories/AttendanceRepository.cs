using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Repositories
{
    public class AttendanceRepository : IAttendanceRepository
    {
        public readonly ApplicationDbContext _context;
        public AttendanceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Attendance>> GetAllAttendencesAsync()
        {
            return await Task.FromResult(_context.Attendances.ToList());
        }

        public async Task<Attendance?> GetAttendanceByIdAsync(Guid attendanceId)
        {
            return await Task.FromResult(_context.Attendances.FirstOrDefault(a => a.AttendanceId == attendanceId));
        }

        public async Task AddAttendanceAsync(Attendance attendance)
        {
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAttendanceAsync(Attendance attendance)
        {
            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAttendanceAsync(Guid attendanceId)
        {
            var attendance = await GetAttendanceByIdAsync(attendanceId);
            if (attendance != null)
            {
                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();
            }
        }
    }
}


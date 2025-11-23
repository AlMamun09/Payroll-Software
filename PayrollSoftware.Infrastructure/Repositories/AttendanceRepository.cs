using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

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
            return await Task.FromResult(
                _context.Attendances.FirstOrDefault(a => a.AttendanceId == attendanceId)
            );
        }

        public async Task AddAttendanceAsync(Attendance attendance)
        {
            await ValidateAttendanceAsync(attendance, isNew: true);
            await CalculateAttendanceFieldsAsync(attendance);

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAttendanceAsync(Attendance attendance)
        {
            await ValidateAttendanceAsync(attendance, isNew: false);
            await CalculateAttendanceFieldsAsync(attendance);

            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();
        }

        private async Task ValidateAttendanceAsync(Attendance attendance, bool isNew)
        {
            if (attendance == null)
                throw new ArgumentNullException(nameof(attendance));

            var errors = new List<string>();

            // Normalize attendance date to date-only
            attendance.AttendanceDate = attendance.AttendanceDate.Date;

            // Employee must exist
            var employee = await _context
                .Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == attendance.EmployeeId);

            if (employee == null)
            {
                errors.Add("Employee not found.");
            }
            else
            {
                // Joining Date Restriction: Disallow attendance before employee's joining date
                if (attendance.AttendanceDate < employee.JoiningDate.Date)
                {
                    errors.Add(
                        $"Attendance date cannot be before employee's joining date ({employee.JoiningDate:yyyy-MM-dd})."
                    );
                }

                // Attendance Validation: Prevent attendance if employee is resigned or on leave
                if (
                    employee.Status != null
                    && (
                        employee.Status.Equals("Resigned", StringComparison.OrdinalIgnoreCase)
                        || employee.Status.Equals("On Leave", StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    errors.Add(
                        "Cannot submit attendance for an employee who is resigned or on leave."
                    );
                }

                // Attendance Validation: Prevent attendance if employee is on approved leave
                var isOnLeave = await _context
                    .Leaves.AsNoTracking()
                    .AnyAsync(l =>
                        l.EmployeeId == attendance.EmployeeId
                        && l.LeaveStatus == "Approved"
                        && attendance.AttendanceDate >= l.StartDate.Date
                        && attendance.AttendanceDate <= l.EndDate.Date
                    );

                if (isOnLeave)
                {
                    errors.Add(
                        $"Cannot submit attendance for {attendance.AttendanceDate:yyyy-MM-dd}. Employee is on approved leave for this date."
                    );
                }

                // Shift Information: Automatically assign shift based on employee details
                if (employee.ShiftId.HasValue && employee.ShiftId.Value != Guid.Empty)
                {
                    // Auto-assign employee's shift if not provided
                    if (attendance.ShiftId == Guid.Empty)
                    {
                        attendance.ShiftId = employee.ShiftId.Value;
                    }
                }
                else
                {
                    // Employee has no assigned shift
                    if (attendance.ShiftId == Guid.Empty)
                    {
                        errors.Add(
                            "Employee has no assigned shift. Please assign a shift to the employee first."
                        );
                    }
                }
            }

            // Shift must exist and be active
            if (attendance.ShiftId != Guid.Empty)
            {
                var shift = await _context
                    .Shifts.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ShiftId == attendance.ShiftId);

                if (shift == null)
                {
                    errors.Add("Invalid shift selected.");
                }
                else if (!shift.IsActive)
                {
                    errors.Add("Selected shift is not active.");
                }
            }

            // --- FIXED: Relaxed In/Out Time Validation ---
            // If InTime is present, we check logic. We NO LONGER force OutTime to be present.
            if (attendance.InTime.HasValue)
            {
                // Only validate duration if BOTH InTime and OutTime exist.
                // If OutTime is null, it's considered an "Incomplete" punch (WorkingHours = 0) which is valid.
                if (attendance.OutTime.HasValue)
                {
                    var timeDiff = attendance.OutTime.Value - attendance.InTime.Value;

                    // Handle overnight shift crossing midnight
                    if (timeDiff < TimeSpan.Zero)
                    {
                        timeDiff = timeDiff.Add(TimeSpan.FromHours(24));
                    }

                    if (timeDiff > TimeSpan.FromHours(24))
                    {
                        errors.Add("Working hours cannot exceed 24 hours.");
                    }
                }
            }

            // Check for duplicate attendance for same employee on same date (only for new records)
            if (isNew)
            {
                var duplicateExists = await _context
                    .Attendances.AsNoTracking()
                    .AnyAsync(a =>
                        a.EmployeeId == attendance.EmployeeId
                        && a.AttendanceDate.Date == attendance.AttendanceDate.Date
                    );

                if (duplicateExists)
                {
                    errors.Add(
                        $"Attendance record already exists for this employee on {attendance.AttendanceDate:yyyy-MM-dd}."
                    );
                }
            }

            // Throw all validation errors
            if (errors.Any())
            {
                throw new ArgumentException(string.Join("\n", errors));
            }
        }

        private async Task CalculateAttendanceFieldsAsync(Attendance attendance)
        {
            // 1. Attendance Status: If either InTime or OutTime exists → mark as Present, else Absent
            if (attendance.InTime.HasValue || attendance.OutTime.HasValue)
            {
                attendance.Status = "Present";
            }
            else
            {
                attendance.Status = "Absent";
                attendance.WorkingHours = 0;
                attendance.LateEntry = null;
                attendance.EarlyLeave = null;
                return; // No need to calculate further for absent
            }

            // 2. Working Hours: Automatically calculate total WorkingHours = OutTime - InTime
            if (attendance.InTime.HasValue && attendance.OutTime.HasValue)
            {
                var timeDiff = attendance.OutTime.Value - attendance.InTime.Value;

                // Handle overnight shifts (when OutTime < InTime)
                if (timeDiff < TimeSpan.Zero)
                {
                    timeDiff = timeDiff.Add(TimeSpan.FromHours(24));
                }

                // Convert to decimal hours (e.g., 8.5 hours)
                attendance.WorkingHours = (decimal)timeDiff.TotalHours;

                // 3. Late Entry & Early Leave: Auto-calculate from shift start/end times
                var shift = await _context
                    .Shifts.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ShiftId == attendance.ShiftId);

                if (shift != null)
                {
                    // Calculate Late Entry
                    if (attendance.InTime.Value > shift.StartTime)
                    {
                        var lateBy = attendance.InTime.Value - shift.StartTime;
                        attendance.LateEntry = lateBy;
                    }
                    else
                    {
                        attendance.LateEntry = null; // On time or early
                    }

                    // Calculate Early Leave
                    // Handle overnight shifts for EndTime
                    var expectedOutTime = shift.EndTime;
                    var actualOutTime = attendance.OutTime.Value;

                    // If shift ends after midnight (overnight shift)
                    if (shift.EndTime < shift.StartTime)
                    {
                        // For overnight shifts, if OutTime is less than StartTime, it's the next day
                        if (actualOutTime < shift.StartTime)
                        {
                            // OutTime is on the next day (after midnight)
                            // Compare directly
                            if (actualOutTime < shift.EndTime)
                            {
                                var earlyBy = shift.EndTime - actualOutTime;
                                attendance.EarlyLeave = earlyBy;
                            }
                            else
                            {
                                attendance.EarlyLeave = null; // On time or late departure
                            }
                        }
                        else
                        {
                            // OutTime is on the same day (before midnight), definitely early
                            var timeUntilMidnight = TimeSpan.FromHours(24) - actualOutTime;
                            var earlyBy = timeUntilMidnight + shift.EndTime;
                            attendance.EarlyLeave = earlyBy;
                        }
                    }
                    else
                    {
                        // Regular shift (same day)
                        if (actualOutTime < expectedOutTime)
                        {
                            var earlyBy = expectedOutTime - actualOutTime;
                            attendance.EarlyLeave = earlyBy;
                        }
                        else
                        {
                            attendance.EarlyLeave = null; // On time or late departure
                        }
                    }
                }
            }
            else
            {
                // Incomplete time entries
                attendance.WorkingHours = 0;
                attendance.LateEntry = null;
                attendance.EarlyLeave = null;
            }
        }

        public async Task<Guid> SaveImportFileAsync(AttendanceImportFile importFile)
        {
            _context.AttendanceImportFiles.Add(importFile);
            await _context.SaveChangesAsync();
            return importFile.ImportId;
        }

        public async Task UpdateImportProgressAsync(
            Guid importId,
            int processed,
            int total,
            string status,
            string? errors = null
        )
        {
            var file = await _context.AttendanceImportFiles.FindAsync(importId);
            if (file != null)
            {
                file.ProcessedRows = processed;
                if (total > 0)
                    file.TotalRows = total;
                file.Status = status;
                if (errors != null)
                    file.ErrorLog = errors;
                if (status == "Completed" || status == "Failed")
                    file.CompletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
        }

        public async Task<AttendanceImportFile?> GetImportStatusAsync(Guid importId)
        {
            return await _context
                .AttendanceImportFiles.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ImportId == importId);
        }
    }
}

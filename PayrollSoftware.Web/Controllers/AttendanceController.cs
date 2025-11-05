using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Web.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ILeaveRepository _leaveRepository;
        private readonly ApplicationDbContext _context;

        public AttendanceController(
            IAttendanceRepository attendanceRepository,
            ILeaveRepository leaveRepository,
            ApplicationDbContext context
        )
        {
            _attendanceRepository = attendanceRepository;
            _leaveRepository = leaveRepository;
            _context = context;
        }

        // GET: /Attendance
        [HttpGet]
        public IActionResult Index() => View();

        // GET: /Attendance/GetAttendancesJson
        [HttpGet]
        public async Task<IActionResult> GetAttendancesJson()
        {
            try
            {
                var attendances = await _attendanceRepository.GetAllAttendencesAsync();

                // Optional filters from DataTables request
                var statusFilter = (
                    Request.Query["statusFilter"].ToString() ?? string.Empty
                ).Trim();
                var dateFilter = Request.Query["dateFilter"].ToString();

                if (!string.IsNullOrWhiteSpace(statusFilter))
                    attendances = attendances
                        .Where(a =>
                            string.Equals(
                                a.Status,
                                statusFilter,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .ToList();

                if (
                    !string.IsNullOrWhiteSpace(dateFilter)
                    && DateTime.TryParse(dateFilter, out var filterDate)
                )
                    attendances = attendances
                        .Where(a => a.AttendanceDate.Date == filterDate.Date)
                        .ToList();

                // Enrich with employee and shift information
                var empLookup = await _context
                    .Employees.AsNoTracking()
                    .ToDictionaryAsync(e => e.EmployeeId, e => new { e.EmployeeCode, e.FullName });

                var shiftLookup = await _context
                    .Shifts.AsNoTracking()
                    .ToDictionaryAsync(
                        s => s.ShiftId,
                        s => new
                        {
                            s.ShiftName,
                            s.StartTime,
                            s.EndTime,
                        }
                    );

                var data = attendances
                    .Select(a => new
                    {
                        a.AttendanceId,
                        a.EmployeeId,
                        EmployeeCode = empLookup.TryGetValue(a.EmployeeId, out var emp)
                            ? (emp.EmployeeCode ?? "N/A")
                            : "N/A",
                        EmployeeName = empLookup.TryGetValue(a.EmployeeId, out var emp2)
                            ? (emp2.FullName ?? "Unknown")
                            : "Unknown",
                        a.ShiftId,
                        ShiftName = shiftLookup.TryGetValue(a.ShiftId, out var shift)
                            ? (shift.ShiftName ?? "N/A")
                            : "N/A",
                        a.AttendanceDate,
                        InTime = a.InTime?.ToString(@"hh\:mm"),
                        OutTime = a.OutTime?.ToString(@"hh\:mm"),
                        a.Status,
                        a.WorkingHours,
                        LateEntry = a.LateEntry?.ToString(@"hh\:mm"),
                        EarlyLeave = a.EarlyLeave?.ToString(@"hh\:mm"),
                    })
                    .ToList();

                return Json(new { data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: /Attendance/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var initialDate = DateTime.Today;
            await PopulateDropdownsAsync(initialDate);
            ViewBag.Title = "Create Attendance";
            ViewBag.FormAction = Url.Action(nameof(Create));
            return View(new AttendanceDto { AttendanceDate = initialDate });
        }

        // POST: /Attendance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] AttendanceDto attendanceDto)
        {
            try
            {
                // Do not bind AttendanceId, Status, WorkingHours, LateEntry, EarlyLeave on create
                ModelState.Remove(nameof(AttendanceDto.AttendanceId));
                ModelState.Remove(nameof(AttendanceDto.Status));
                ModelState.Remove(nameof(AttendanceDto.WorkingHours));
                ModelState.Remove(nameof(AttendanceDto.LateEntry));
                ModelState.Remove(nameof(AttendanceDto.EarlyLeave));

                if (attendanceDto.EmployeeId == Guid.Empty)
                    ModelState.AddModelError(
                        nameof(AttendanceDto.EmployeeId),
                        "Please select an employee."
                    );

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();
                    return BadRequest(new { success = false, message = string.Join("\n", errors) });
                }

                var entity = new Attendance
                {
                    AttendanceId = Guid.NewGuid(),
                    EmployeeId = attendanceDto.EmployeeId,
                    ShiftId = attendanceDto.ShiftId,
                    AttendanceDate = attendanceDto.AttendanceDate.Date,
                    InTime = attendanceDto.InTime,
                    OutTime = attendanceDto.OutTime,
                    // Status, WorkingHours, LateEntry, EarlyLeave will be calculated by repository
                };

                await _attendanceRepository.AddAttendanceAsync(entity);
                return Json(
                    new
                    {
                        success = true,
                        message = "Attendance recorded successfully.",
                        id = entity.AttendanceId,
                    }
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { success = false, message = $"Error creating attendance: {ex.Message}" }
                );
            }
        }

        // GET: /Attendance/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var attendance = await _attendanceRepository.GetAttendanceByIdAsync(id);
                if (attendance == null)
                    return NotFound();

                await PopulateDropdownsAsync(attendance.AttendanceDate, attendance.EmployeeId);
                ViewBag.Title = "Edit Attendance";
                ViewBag.FormAction = Url.Action(nameof(Edit), new { id });
                return View("Create", MapToDto(attendance));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // POST: /Attendance/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [FromForm] AttendanceDto attendanceDto)
        {
            try
            {
                if (id != attendanceDto.AttendanceId)
                    return BadRequest(new { success = false, message = "Attendance ID mismatch." });

                // Remove calculated fields from validation
                ModelState.Remove(nameof(AttendanceDto.Status));
                ModelState.Remove(nameof(AttendanceDto.WorkingHours));
                ModelState.Remove(nameof(AttendanceDto.LateEntry));
                ModelState.Remove(nameof(AttendanceDto.EarlyLeave));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();
                    return BadRequest(new { success = false, message = string.Join("\n", errors) });
                }

                var entity = new Attendance
                {
                    AttendanceId = attendanceDto.AttendanceId,
                    EmployeeId = attendanceDto.EmployeeId,
                    ShiftId = attendanceDto.ShiftId,
                    AttendanceDate = attendanceDto.AttendanceDate.Date,
                    InTime = attendanceDto.InTime,
                    OutTime = attendanceDto.OutTime,
                    // Status, WorkingHours, LateEntry, EarlyLeave will be recalculated by repository
                };

                await _attendanceRepository.UpdateAttendanceAsync(entity);
                return Json(
                    new
                    {
                        success = true,
                        message = "Attendance updated successfully.",
                        id,
                    }
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { success = false, message = $"Error updating attendance: {ex.Message}" }
                );
            }
        }

        // POST: /Attendance/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _attendanceRepository.DeleteAttendanceAsync(id);
                return Json(new { success = true, message = "Attendance deleted successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { success = false, message = $"Error deleting attendance: {ex.Message}" }
                );
            }
        }

        // GET: /Attendance/GetAvailableEmployees
        [HttpGet]
        public async Task<IActionResult> GetAvailableEmployees(DateTime attendanceDate)
        {
            try
            {
                var checkDate = attendanceDate.Date;

                // Get all active employees
                var employees = await _context
                    .Employees.AsNoTracking()
                    .Where(e => e.Status == "Active")
                    .OrderBy(e => e.EmployeeCode)
                    .Select(e => new
                    {
                        e.EmployeeId,
                        EmployeeCode = e.EmployeeCode ?? "N/A",
                        FullName = e.FullName ?? "Unknown",
                        e.ShiftId,
                        e.JoiningDate,
                        e.Status,
                    })
                    .ToListAsync();

                // Get all approved leaves that overlap with the attendance date
                var approvedLeaves = await _context
                    .Leaves.AsNoTracking()
                    .Where(l =>
                        l.LeaveStatus == "Approved"
                        && l.StartDate.Date <= checkDate
                        && l.EndDate.Date >= checkDate
                    )
                    .Select(l => l.EmployeeId)
                    .ToListAsync();

                // Filter out employees on approved leave
                var availableEmployees = employees
                    .Where(e => !approvedLeaves.Contains(e.EmployeeId))
                    .Select(e => new
                    {
                        e.EmployeeId,
                        e.EmployeeCode,
                        e.FullName,
                        e.ShiftId,
                        JoiningDate = e.JoiningDate.ToString("yyyy-MM-dd"),
                        e.Status,
                    })
                    .ToList();

                return Json(new { success = true, employees = availableEmployees });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private AttendanceDto MapToDto(Attendance attendance) =>
            new AttendanceDto
            {
                AttendanceId = attendance.AttendanceId,
                EmployeeId = attendance.EmployeeId,
                ShiftId = attendance.ShiftId,
                AttendanceDate = attendance.AttendanceDate,
                InTime = attendance.InTime,
                OutTime = attendance.OutTime,
                Status = attendance.Status,
                WorkingHours = attendance.WorkingHours,
                LateEntry = attendance.LateEntry,
                EarlyLeave = attendance.EarlyLeave,
            };

        private async Task PopulateDropdownsAsync(
            DateTime? attendanceDate = null,
            Guid? currentEmployeeId = null
        )
        {
            // Get all active employees
            var employees = await _context
                .Employees.AsNoTracking()
                .Where(e => e.Status == "Active")
                .OrderBy(e => e.EmployeeCode)
                .Select(e => new
                {
                    e.EmployeeId,
                    EmployeeCode = e.EmployeeCode ?? "N/A",
                    FullName = e.FullName ?? "Unknown",
                    e.ShiftId,
                    e.JoiningDate,
                    e.Status,
                })
                .ToListAsync();

            // If attendanceDate is provided, filter out employees on approved leave
            if (attendanceDate.HasValue)
            {
                var checkDate = attendanceDate.Value.Date;

                // Get all approved leaves that overlap with the attendance date
                var approvedLeaves = await _context
                    .Leaves.AsNoTracking()
                    .Where(l =>
                        l.LeaveStatus == "Approved"
                        && l.StartDate.Date <= checkDate
                        && l.EndDate.Date >= checkDate
                    )
                    .Select(l => l.EmployeeId)
                    .ToListAsync();

                // Filter out employees on approved leave, but keep the current employee if editing
                employees = employees
                    .Where(e =>
                        !approvedLeaves.Contains(e.EmployeeId)
                        || (currentEmployeeId.HasValue && e.EmployeeId == currentEmployeeId.Value)
                    )
                    .ToList();
            }

            ViewBag.Employees = employees;

            ViewBag.Shifts = await _context
                .Shifts.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.ShiftName)
                .Select(s => new
                {
                    s.ShiftId,
                    s.ShiftName,
                    s.StartTime,
                    s.EndTime,
                })
                .ToListAsync();
        }
    }
}

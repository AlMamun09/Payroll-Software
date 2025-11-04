using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PayrollSoftware.Web.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ApplicationDbContext _context;

        public AttendanceController(IAttendanceRepository attendanceRepository, ApplicationDbContext context)
        {
            _attendanceRepository = attendanceRepository;
            _context = context;
        }

        // GET: /Attendance
        [HttpGet]
        public IActionResult Index() => View();

        // GET: /Attendance/GetAttendanceJson
        [HttpGet]
        public async Task<IActionResult> GetAttendanceJson()
        {
            var records = await _attendanceRepository.GetAllAttendanceAsync();

            var empLookup = await _context.Employees.AsNoTracking()
                .ToDictionaryAsync(e => e.EmployeeId, e => e.FullName);

            var data = records.Select(a => new
            {
                a.AttendanceId,
                a.EmployeeId,
                EmployeeName = empLookup.ContainsKey(a.EmployeeId) ? empLookup[a.EmployeeId] : "Unknown",
                a.AttendanceDate,
                a.InTime,
                a.OutTime,
                a.WorkingHours,
                a.Status,
                a.LateEntry,
                a.EarlyLeave
            }).ToList();

            return Json(new { data });
        }

        // GET: /Attendance/Create
        [HttpGet]
        public async Task<IActionResult> Create() // Changed to async Task
        {
            // ADD THIS LINE
            await LoadEmployeesForDropdown();

            ViewBag.Title = "Record Attendance";
            return PartialView("Create", new AttendanceDto { AttendanceDate = DateTime.Today });
        }

        // POST: /Attendance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] AttendanceDto dto)
        {
            try
            {
                var entity = new Attendance
                {
                    AttendanceId = Guid.NewGuid(),
                    EmployeeId = dto.EmployeeId,
                    AttendanceDate = dto.AttendanceDate.Date,
                    InTime = dto.InTime,
                    OutTime = dto.OutTime,
                };

                var created = await _attendanceRepository.RecordAttendanceAsync(entity);
                return Json(new { success = true, message = "Attendance recorded successfully.", id = created.AttendanceId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET: /Attendance/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var attendance = await _attendanceRepository.GetAttendanceByIdAsync(id);
            if (attendance == null) return NotFound();

            // ADD THIS LINE
            await LoadEmployeesForDropdown();

            return PartialView("Create", new AttendanceDto
            {
                AttendanceId = attendance.AttendanceId,
                EmployeeId = attendance.EmployeeId,
                AttendanceDate = attendance.AttendanceDate,
                InTime = attendance.InTime,
                OutTime = attendance.OutTime,
                Status = attendance.Status,
                WorkingHours = attendance.WorkingHours
            });
        }

        // POST: /Attendance/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [FromForm] AttendanceDto dto)
        {
            try
            {
                if (id != dto.AttendanceId)
                    return BadRequest(new { success = false, message = "Attendance ID mismatch." });

                var entity = new Attendance
                {
                    AttendanceId = dto.AttendanceId,
                    EmployeeId = dto.EmployeeId,
                    AttendanceDate = dto.AttendanceDate.Date,
                    InTime = dto.InTime,
                    OutTime = dto.OutTime
                };

                await _attendanceRepository.UpdateAttendanceAsync(entity);
                return Json(new { success = true, message = "Attendance updated successfully.", id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
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
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ADD THIS PRIVATE METHOD
        private async Task LoadEmployeesForDropdown()
        {
            var employees = await _context.Employees
                .Where(e => e.Status == "Currently Active") // Only active employees
                .Select(e => new
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName
                })
                .OrderBy(e => e.FullName)
                .ToListAsync();

            ViewBag.Employees = employees;
        }
    }
}
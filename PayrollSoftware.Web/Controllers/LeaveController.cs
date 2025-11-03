using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PayrollSoftware.Web.Controllers
{
    public class LeaveController : Controller
    {
        private readonly ILeaveRepository _leaveRepository;
        private readonly ApplicationDbContext _context;

        public LeaveController(ILeaveRepository leaveRepository, ApplicationDbContext context)
        {
            _leaveRepository = leaveRepository;
            _context = context;
        }

        // GET: /Leave
        [HttpGet]
        public IActionResult Index() => View();

        // GET: /Leave/GetLeavesJson
        [HttpGet]
        public async Task<IActionResult> GetLeavesJson()
        {
            try
            {
                var leaves = await _leaveRepository.GetAllLeavesAsync();

                // Optional filters from DataTables request
                var statusFilter = (Request.Query["statusFilter"].ToString() ?? string.Empty).Trim();
                var typeFilter = (Request.Query["typeFilter"].ToString() ?? string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(statusFilter))
                    leaves = leaves.Where(l => string.Equals(l.LeaveStatus, statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                if (!string.IsNullOrWhiteSpace(typeFilter))
                    leaves = leaves.Where(l => string.Equals(l.LeaveType, typeFilter, StringComparison.OrdinalIgnoreCase)).ToList();

                // Enrich with employee code and name separately
                var empLookup = await _context.Employees.AsNoTracking()
                    .ToDictionaryAsync(e => e.EmployeeId, e => new { e.EmployeeCode, e.FullName });

                var data = leaves.Select(l => new
                {
                    l.LeaveId,
                    l.EmployeeId,
                    EmployeeCode = empLookup.TryGetValue(l.EmployeeId, out var emp) ? (emp.EmployeeCode ?? "N/A") : "N/A",
                    EmployeeName = empLookup.TryGetValue(l.EmployeeId, out var emp2) ? (emp2.FullName ?? "Unknown") : "Unknown",
                    l.LeaveType,
                    l.StartDate,
                    l.EndDate,
                    l.TotalDays,
                    l.LeaveStatus,
                    l.Remarks
                }).ToList();

                return Json(new { data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: /Leave/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateEmployeesAsync();
            ViewBag.Title = "Create Leave";
            ViewBag.FormAction = Url.Action(nameof(Create));
            return PartialView("Create", new LeaveDto { StartDate = DateTime.Today, EndDate = DateTime.Today });
        }

        // POST: /Leave/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] LeaveDto leaveDto)
        {
            try
            {
                // Do not bind LeaveId and TotalDays on create; compute/set server-side
                ModelState.Remove(nameof(LeaveDto.LeaveId));
                ModelState.Remove(nameof(LeaveDto.TotalDays));

                if (leaveDto.EmployeeId == Guid.Empty)
                    ModelState.AddModelError(nameof(LeaveDto.EmployeeId), "Please select an employee.");
                if (leaveDto.EndDate.Date < leaveDto.StartDate.Date)
                    ModelState.AddModelError(string.Empty, "End date cannot be before start date.");

                // Validate LeaveStatus is provided
                if (string.IsNullOrWhiteSpace(leaveDto.LeaveStatus))
                    ModelState.AddModelError(nameof(LeaveDto.LeaveStatus), "Please select a leave status.");

                // Overlap check when inputs valid
                if (ModelState.IsValid)
                {
                    var overlap = await _leaveRepository.HasLeaveOverlapAsync(leaveDto.EmployeeId, leaveDto.StartDate.Date, leaveDto.EndDate.Date, null);
                    if (overlap)
                        ModelState.AddModelError(string.Empty, "Selected dates overlap with an existing leave.");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                    return BadRequest(new { success = false, message = string.Join("\n", errors) });
                }

                var entity = new Leave
                {
                    LeaveId = Guid.NewGuid(),
                    EmployeeId = leaveDto.EmployeeId,
                    LeaveType = leaveDto.LeaveType,
                    StartDate = leaveDto.StartDate.Date,
                    EndDate = leaveDto.EndDate.Date,
                    TotalDays = CalculateBusinessDays(leaveDto.StartDate.Date, leaveDto.EndDate.Date),
                    LeaveStatus = leaveDto.LeaveStatus, // Use the status from the form
                    Remarks = leaveDto.Remarks
                };

                var createdLeave = await _leaveRepository.ApplyForLeaveAsync(entity);
                return Json(new { success = true, message = "Leave application submitted successfully.", id = createdLeave.LeaveId });
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
                return StatusCode(500, new { success = false, message = $"Error applying for leave: {ex.Message}" });
            }
        }

        // GET: /Leave/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var leave = await _leaveRepository.GetLeaveByIdAsync(id);
                if (leave == null) return NotFound();

                await PopulateEmployeesAsync();
                ViewBag.Title = "Edit Leave";
                ViewBag.FormAction = Url.Action(nameof(Edit));
                return PartialView("Create", MapToDto(leave));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // POST: /Leave/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [FromForm] LeaveDto leaveDto)
        {
            try
            {
                if (id != leaveDto.LeaveId)
                    return BadRequest(new { success = false, message = "Leave ID mismatch." });

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                    return BadRequest(new { success = false, message = string.Join("\n", errors) });
                }

                // Only status/remarks are updated according to repository method signature
                await _leaveRepository.UpdateLeaveStatusAsync(leaveDto.LeaveId, leaveDto.LeaveStatus ?? "Pending", leaveDto.Remarks ?? string.Empty);
                return Json(new { success = true, message = "Leave updated successfully.", id });
            }
            catch (ArgumentException ex)
            { return BadRequest(new { success = false, message = ex.Message }); }
            catch (InvalidOperationException ex)
            { return BadRequest(new { success = false, message = ex.Message }); }
            catch (Exception ex)
            { return StatusCode(500, new { success = false, message = $"Error updating leave: {ex.Message}" }); }
        }

        // POST: /Leave/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _leaveRepository.DeleteLeaveAsync(id);
                return Json(new { success = true, message = "Leave deleted successfully." });
            }
            catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { success = false, message = $"Error deleting leave: {ex.Message}" }); }
        }

        // GET: /Leave/CheckOverlap
        [HttpGet]
        public async Task<IActionResult> CheckOverlap(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeLeaveId = null)
        {
            try
            {
                var hasOverlap = await _leaveRepository.HasLeaveOverlapAsync(employeeId, startDate.Date, endDate.Date, excludeLeaveId);
                return Json(new { success = true, hasOverlap });
            }
            catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
        }

        // GET: /Leave/GetEmployeeName/{id}
        [HttpGet]
        public async Task<IActionResult> GetEmployeeName(Guid id)
        {
            try
            {
                var employee = await _context.Employees.AsNoTracking()
                    .Where(e => e.EmployeeId == id)
                    .Select(e => new { e.FullName })
                    .FirstOrDefaultAsync();

                if (employee == null)
                    return Json(new { success = false, name = "" });

                return Json(new { success = true, name = employee.FullName ?? "" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        private static int CalculateBusinessDays(DateTime start, DateTime end)
        {
            if (end < start) return 0;
            var total = 0;
            for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            {
                var day = d.DayOfWeek;
                // Sunday-Thursday are business days (0=Sunday, 4=Thursday)
                if (day != DayOfWeek.Friday && day != DayOfWeek.Saturday)
                    total++;
            }
            return total;
        }

        private LeaveDto MapToDto(Leave leave) => new LeaveDto
        {
            LeaveId = leave.LeaveId,
            EmployeeId = leave.EmployeeId,
            LeaveType = leave.LeaveType,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            TotalDays = leave.TotalDays,
            LeaveStatus = leave.LeaveStatus,
            Remarks = leave.Remarks
        };

        private async Task PopulateEmployeesAsync()
        {
            ViewBag.Employees = await _context.Employees.AsNoTracking()
                .OrderBy(e => e.EmployeeCode)
                .Select(e => new
                {
                    e.EmployeeId,
                    EmployeeCode = e.EmployeeCode ?? "N/A",
                    FullName = e.FullName ?? "Unknown"
                })
                .ToListAsync();
        }
    }
}
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

   // GET: /Attendance/GetAttendancesJson
        [HttpGet]
      public async Task<IActionResult> GetAttendancesJson()
   {
          try
         {
        var attendances = await _attendanceRepository.GetAllAttendencesAsync();

    // Optional filters from DataTables request
      var statusFilter = (Request.Query["statusFilter"].ToString() ?? string.Empty).Trim();
          var dateFilter = Request.Query["dateFilter"].ToString();

        if (!string.IsNullOrWhiteSpace(statusFilter))
        attendances = attendances.Where(a => string.Equals(a.Status, statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();

              if (!string.IsNullOrWhiteSpace(dateFilter) && DateTime.TryParse(dateFilter, out var filterDate))
 attendances = attendances.Where(a => a.AttendanceDate.Date == filterDate.Date).ToList();

       // Enrich with employee and shift information
   var empLookup = await _context.Employees.AsNoTracking()
          .ToDictionaryAsync(e => e.EmployeeId, e => new { e.EmployeeCode, e.FullName });

      var shiftLookup = await _context.Shifts.AsNoTracking()
           .ToDictionaryAsync(s => s.ShiftId, s => new { s.ShiftName, s.StartTime, s.EndTime });

         var data = attendances.Select(a => new
  {
        a.AttendanceId,
       a.EmployeeId,
       EmployeeCode = empLookup.TryGetValue(a.EmployeeId, out var emp) ? (emp.EmployeeCode ?? "N/A") : "N/A",
       EmployeeName = empLookup.TryGetValue(a.EmployeeId, out var emp2) ? (emp2.FullName ?? "Unknown") : "Unknown",
        a.ShiftId,
   ShiftName = shiftLookup.TryGetValue(a.ShiftId, out var shift) ? (shift.ShiftName ?? "N/A") : "N/A",
         a.AttendanceDate,
         InTime = a.InTime?.ToString(@"hh\:mm"),
    OutTime = a.OutTime?.ToString(@"hh\:mm"),
    a.Status,
              a.WorkingHours,
             LateEntry = a.LateEntry?.ToString(@"hh\:mm"),
     EarlyLeave = a.EarlyLeave?.ToString(@"hh\:mm")
  }).ToList();

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
     await PopulateDropdownsAsync();
       ViewBag.Title = "Create Attendance";
       ViewBag.FormAction = Url.Action(nameof(Create));
    return View(new AttendanceDto { AttendanceDate = DateTime.Today });
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
 ModelState.AddModelError(nameof(AttendanceDto.EmployeeId), "Please select an employee.");

 if (!ModelState.IsValid)
   {
      var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            return BadRequest(new { success = false, message = string.Join("\n", errors) });
        }

       var entity = new Attendance
          {
    AttendanceId = Guid.NewGuid(),
             EmployeeId = attendanceDto.EmployeeId,
        ShiftId = attendanceDto.ShiftId,
           AttendanceDate = attendanceDto.AttendanceDate.Date,
         InTime = attendanceDto.InTime,
         OutTime = attendanceDto.OutTime
            // Status, WorkingHours, LateEntry, EarlyLeave will be calculated by repository
            };

         await _attendanceRepository.AddAttendanceAsync(entity);
        return Json(new { success = true, message = "Attendance recorded successfully.", id = entity.AttendanceId });
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
             return StatusCode(500, new { success = false, message = $"Error creating attendance: {ex.Message}" });
   }
        }

        // GET: /Attendance/Edit/{id}
        [HttpGet]
     public async Task<IActionResult> Edit(Guid id)
 {
      try
 {
        var attendance = await _attendanceRepository.GetAttendanceByIdAsync(id);
    if (attendance == null) return NotFound();

        await PopulateDropdownsAsync();
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
 var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
        return BadRequest(new { success = false, message = string.Join("\n", errors) });
       }

  var entity = new Attendance
        {
     AttendanceId = attendanceDto.AttendanceId,
   EmployeeId = attendanceDto.EmployeeId,
        ShiftId = attendanceDto.ShiftId,
     AttendanceDate = attendanceDto.AttendanceDate.Date,
       InTime = attendanceDto.InTime,
     OutTime = attendanceDto.OutTime
         // Status, WorkingHours, LateEntry, EarlyLeave will be recalculated by repository
              };

     await _attendanceRepository.UpdateAttendanceAsync(entity);
                return Json(new { success = true, message = "Attendance updated successfully.", id });
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
        return StatusCode(500, new { success = false, message = $"Error updating attendance: {ex.Message}" });
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
   return StatusCode(500, new { success = false, message = $"Error deleting attendance: {ex.Message}" });
            }
    }

  private AttendanceDto MapToDto(Attendance attendance) => new AttendanceDto
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
      EarlyLeave = attendance.EarlyLeave
        };

        private async Task PopulateDropdownsAsync()
   {
        ViewBag.Employees = await _context.Employees.AsNoTracking()
     .Where(e => e.Status != "Resigned") // Only show active employees
             .OrderBy(e => e.EmployeeCode)
       .Select(e => new
          {
      e.EmployeeId,
        EmployeeCode = e.EmployeeCode ?? "N/A",
      FullName = e.FullName ?? "Unknown",
    e.ShiftId,
           e.JoiningDate,
         e.Status
      })
    .ToListAsync();

  ViewBag.Shifts = await _context.Shifts.AsNoTracking()
   .Where(s => s.IsActive)
      .OrderBy(s => s.ShiftName)
             .Select(s => new
             {
s.ShiftId,
      s.ShiftName,
  s.StartTime,
      s.EndTime
       })
        .ToListAsync();
        }
    }
}

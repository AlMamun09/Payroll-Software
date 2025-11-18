using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

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

        // GET: /Leave/PendingLeaves
        [HttpGet]
        public IActionResult PendingLeaves() => View();

        // GET: /Leave/ApprovedLeaves
        [HttpGet]
        public IActionResult ApprovedLeaves() => View();

        // GET: /Leave/GetPendingLeavesJson
        [HttpGet]
        public async Task<IActionResult> GetPendingLeavesJson()
        {
            try
            {
                var leaves = await _leaveRepository.GetAllLeavesAsync();

                // Filter only pending leaves
                leaves = leaves
                    .Where(l =>
                        string.Equals(l.LeaveStatus, "Pending", StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();

                // Optional type filter
                var typeFilter = (Request.Query["typeFilter"].ToString() ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(typeFilter))
                    leaves = leaves
                        .Where(l =>
                            string.Equals(
                                l.LeaveType,
                                typeFilter,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .ToList();

                // Enrich with employee code and name
                var empLookup = await _context
                    .Employees.AsNoTracking()
                    .ToDictionaryAsync(e => e.EmployeeId, e => new { e.EmployeeCode, e.FullName });

                var data = leaves
                    .Select(l => new
                    {
                        l.LeaveId,
                        l.EmployeeId,
                        EmployeeCode = empLookup.TryGetValue(l.EmployeeId, out var emp)
                            ? (emp.EmployeeCode ?? "N/A")
                            : "N/A",
                        EmployeeName = empLookup.TryGetValue(l.EmployeeId, out var emp2)
                            ? (emp2.FullName ?? "Unknown")
                            : "Unknown",
                        l.LeaveType,
                        l.StartDate,
                        l.EndDate,
                        l.TotalDays,
                        l.LeaveStatus,
                        l.Remarks,
                    })
                    .ToList();

                return Json(new { data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: /Leave/GetApprovedLeavesJson
        [HttpGet]
        public async Task<IActionResult> GetApprovedLeavesJson(string monthFilter = "")
        {
            try
            {
                var leaves = await _leaveRepository.GetAllLeavesAsync();

                // Filter only approved leaves
                leaves = leaves
                    .Where(l =>
                        string.Equals(l.LeaveStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();

                // Split leaves that span multiple months
                var splitLeaves =
                    new List<(
                        Guid EmployeeId,
                        string LeaveType,
                        DateTime StartDate,
                        DateTime EndDate,
                        int Days
                    )>();

                foreach (var leave in leaves)
                {
                    var currentStart = leave.StartDate.Date;
                    var leaveEnd = leave.EndDate.Date;

                    while (currentStart <= leaveEnd)
                    {
                        var monthEnd = new DateTime(
                            currentStart.Year,
                            currentStart.Month,
                            DateTime.DaysInMonth(currentStart.Year, currentStart.Month)
                        );
                        var periodEnd = leaveEnd < monthEnd ? leaveEnd : monthEnd;

                        var days = CalculateBusinessDays(currentStart, periodEnd);

                        splitLeaves.Add(
                            (leave.EmployeeId, leave.LeaveType ?? "", currentStart, periodEnd, days)
                        );

                        // Move to next month
                        currentStart = monthEnd.AddDays(1);
                    }
                }

                // Apply month filter if provided
                if (!string.IsNullOrWhiteSpace(monthFilter))
                {
                    var filterDate = DateTime.Parse(monthFilter + "-01");
                    splitLeaves = splitLeaves
                        .Where(l =>
                            l.StartDate.Year == filterDate.Year
                            && l.StartDate.Month == filterDate.Month
                        )
                        .ToList();
                }

                // Group by employee and month
                var grouped = splitLeaves
                    .GroupBy(l => new { l.EmployeeId, Month = l.StartDate.ToString("yyyy-MM") })
                    .Select(g => new
                    {
                        EmployeeId = g.Key.EmployeeId,
                        Month = g.Key.Month,
                        StartDate = g.Min(l => l.StartDate),
                        EndDate = g.Max(l => l.EndDate),
                        PaidLeaveDays = g.Where(l =>
                                !l.LeaveType.Equals("Unpaid", StringComparison.OrdinalIgnoreCase)
                            )
                            .Sum(l => l.Days),
                        UnpaidLeaveDays = g.Where(l =>
                                l.LeaveType.Equals("Unpaid", StringComparison.OrdinalIgnoreCase)
                            )
                            .Sum(l => l.Days),
                        TotalLeaveDays = g.Sum(l => l.Days),
                    })
                    .ToList();

                // Enrich with employee details
                var empLookup = await _context
                    .Employees.AsNoTracking()
                    .ToDictionaryAsync(e => e.EmployeeId, e => new { e.EmployeeCode, e.FullName });

                var data = grouped
                    .Select(g => new
                    {
                        g.EmployeeId,
                        EmployeeCode = empLookup.TryGetValue(g.EmployeeId, out var emp)
                            ? (emp.EmployeeCode ?? "N/A")
                            : "N/A",
                        EmployeeName = empLookup.TryGetValue(g.EmployeeId, out var emp2)
                            ? (emp2.FullName ?? "Unknown")
                            : "Unknown",
                        g.Month,
                        g.StartDate,
                        g.EndDate,
                        g.PaidLeaveDays,
                        g.UnpaidLeaveDays,
                        g.TotalLeaveDays,
                    })
                    .OrderByDescending(x => x.Month)
                    .ThenBy(x => x.EmployeeName)
                    .ToList();

                return Json(new { data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // POST: /Leave/ApproveLeave/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLeave(Guid id)
        {
            try
            {
                await _leaveRepository.UpdateLeaveStatusAsync(id, "Approved", "");
                return Json(new { success = true, message = "Leave approved successfully." });
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
                    new { success = false, message = $"Error approving leave: {ex.Message}" }
                );
            }
        }

        // POST: /Leave/DenyLeave/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DenyLeave(Guid id)
        {
            try
            {
                await _leaveRepository.UpdateLeaveStatusAsync(id, "Rejected", "");
                return Json(new { success = true, message = "Leave Rejected successfully." });
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
                    new { success = false, message = $"Error Rejecting leave: {ex.Message}" }
                );
            }
        }

        // GET: /Leave/Details/{employeeId}
        [HttpGet]
        [Route("Leave/Details/{employeeId:guid}")]
        public async Task<IActionResult> Details(Guid employeeId, string month = "")
        {
            try
            {
                // Log the incoming parameters for debugging
                Console.WriteLine($"Details called with EmployeeId: {employeeId}, Month: {month}");

                // Get employee details first - Use AsNoTracking for read-only query
                var employee = await _context
                    .Employees.AsNoTracking()
                    .Where(e => e.EmployeeId == employeeId)
                    .Select(e => new { e.EmployeeCode, e.FullName })
                    .FirstOrDefaultAsync();

                Console.WriteLine($"Employee found: {employee?.FullName ?? "NULL"}");

                ViewBag.EmployeeCode = employee?.EmployeeCode ?? "N/A";
                ViewBag.EmployeeName = employee?.FullName ?? "Unknown";

                // Get ALL leaves for this employee with approved status
                var allLeaves = await _leaveRepository.GetAllLeavesAsync();
                Console.WriteLine($"Total leaves in system: {allLeaves.Count()}");

                // Filter by employee and approved status
                var leaves = allLeaves
                    .Where(l =>
                        l.EmployeeId == employeeId
                        && string.Equals(
                            l.LeaveStatus,
                            "Approved",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .ToList();

                Console.WriteLine($"Approved leaves for employee {employeeId}: {leaves.Count}");

                // If month filter is provided, split leaves by month boundaries
                if (!string.IsNullOrWhiteSpace(month))
                {
                    var filterDate = DateTime.Parse(month + "-01");
                    var monthStart = new DateTime(filterDate.Year, filterDate.Month, 1);
                    var monthEnd = new DateTime(
                        filterDate.Year,
                        filterDate.Month,
                        DateTime.DaysInMonth(filterDate.Year, filterDate.Month)
                    );

                    Console.WriteLine($"Filtering by month: {filterDate:yyyy-MM}");
                    Console.WriteLine(
                        $"Month range: {monthStart:yyyy-MM-dd} to {monthEnd:yyyy-MM-dd}"
                    );

                    // Split leaves that span across month boundaries
                    var splitLeaves = new List<Leave>();

                    foreach (var leave in leaves)
                    {
                        // Check if this leave overlaps with the selected month
                        if (leave.StartDate.Date <= monthEnd && leave.EndDate.Date >= monthStart)
                        {
                            // Create a new leave object with dates constrained to the selected month
                            var splitLeave = new Leave
                            {
                                LeaveId = leave.LeaveId,
                                EmployeeId = leave.EmployeeId,
                                LeaveType = leave.LeaveType,
                                LeaveStatus = leave.LeaveStatus,
                                Remarks = leave.Remarks,
                                // Constrain start date to be within the month
                                StartDate =
                                    leave.StartDate < monthStart ? monthStart : leave.StartDate,
                                // Constrain end date to be within the month
                                EndDate = leave.EndDate > monthEnd ? monthEnd : leave.EndDate,
                            };

                            // Recalculate total days for the split period
                            splitLeave.TotalDays = CalculateBusinessDays(
                                splitLeave.StartDate,
                                splitLeave.EndDate
                            );

                            splitLeaves.Add(splitLeave);

                            Console.WriteLine(
                                $"Split leave {leave.LeaveId}: Original {leave.StartDate:yyyy-MM-dd} to {leave.EndDate:yyyy-MM-dd}, Split to {splitLeave.StartDate:yyyy-MM-dd} to {splitLeave.EndDate:yyyy-MM-dd}, Days: {splitLeave.TotalDays}"
                            );
                        }
                    }

                    leaves = splitLeaves;
                    Console.WriteLine($"Leaves after month filter and split: {leaves.Count}");
                    ViewBag.Month = filterDate.ToString("MMMM yyyy");
                }
                else
                {
                    ViewBag.Month = "All Time";
                }

                return PartialView("Details", leaves);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Details: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, ex.Message);
            }
        }

        // GET: /Leave/GetLeavesJson
        [HttpGet]
        public async Task<IActionResult> GetLeavesJson(string filterType = "")
        {
            try
            {
                var leaves = await _leaveRepository.GetAllLeavesAsync();

                // Apply filter based on filterType parameter
                if (!string.IsNullOrWhiteSpace(filterType))
                {
                    if (filterType.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                        leaves = leaves
                            .Where(l =>
                                string.Equals(
                                    l.LeaveStatus,
                                    "Pending",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            .ToList();
                    else if (filterType.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                        leaves = leaves
                            .Where(l =>
                                string.Equals(
                                    l.LeaveStatus,
                                    "Approved",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            .ToList();
                }

                // Optional filters from DataTables request
                var statusFilter = (
                    Request.Query["statusFilter"].ToString() ?? string.Empty
                ).Trim();
                var typeFilter = (Request.Query["typeFilter"].ToString() ?? string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(statusFilter))
                    leaves = leaves
                        .Where(l =>
                            string.Equals(
                                l.LeaveStatus,
                                statusFilter,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .ToList();
                if (!string.IsNullOrWhiteSpace(typeFilter))
                    leaves = leaves
                        .Where(l =>
                            string.Equals(
                                l.LeaveType,
                                typeFilter,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .ToList();

                // Enrich with employee code and name separately
                var empLookup = await _context
                    .Employees.AsNoTracking()
                    .ToDictionaryAsync(e => e.EmployeeId, e => new { e.EmployeeCode, e.FullName });

                var data = leaves
                    .Select(l => new
                    {
                        l.LeaveId,
                        l.EmployeeId,
                        EmployeeCode = empLookup.TryGetValue(l.EmployeeId, out var emp)
                            ? (emp.EmployeeCode ?? "N/A")
                            : "N/A",
                        EmployeeName = empLookup.TryGetValue(l.EmployeeId, out var emp2)
                            ? (emp2.FullName ?? "Unknown")
                            : "Unknown",
                        l.LeaveType,
                        l.StartDate,
                        l.EndDate,
                        l.TotalDays,
                        l.LeaveStatus,
                        l.Remarks,
                    })
                    .ToList();

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
            ViewBag.Title = "Add Leave";
            ViewBag.FormAction = Url.Action(nameof(Create));
            // Return full view instead of PartialView so it uses layout
            return View(
                "Create",
                new LeaveDto { StartDate = DateTime.Today, EndDate = DateTime.Today }
            );
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
                    ModelState.AddModelError(
                        nameof(LeaveDto.EmployeeId),
                        "Please select an employee."
                    );
                if (leaveDto.EndDate.Date < leaveDto.StartDate.Date)
                    ModelState.AddModelError(string.Empty, "End date cannot be before start date.");

                // Validate LeaveStatus is provided
                if (string.IsNullOrWhiteSpace(leaveDto.LeaveStatus))
                    ModelState.AddModelError(
                        nameof(LeaveDto.LeaveStatus),
                        "Please select a leave status."
                    );

                // Overlap check when inputs valid
                if (ModelState.IsValid)
                {
                    var overlap = await _leaveRepository.HasLeaveOverlapAsync(
                        leaveDto.EmployeeId,
                        leaveDto.StartDate.Date,
                        leaveDto.EndDate.Date,
                        null
                    );
                    if (overlap)
                        ModelState.AddModelError(
                            string.Empty,
                            "Selected dates overlap with an existing leave."
                        );
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();
                    return BadRequest(new { success = false, message = string.Join("\n", errors) });
                }

                var entity = new Leave
                {
                    LeaveId = Guid.NewGuid(),
                    EmployeeId = leaveDto.EmployeeId,
                    LeaveType = leaveDto.LeaveType,
                    StartDate = leaveDto.StartDate.Date,
                    EndDate = leaveDto.EndDate.Date,
                    TotalDays = CalculateBusinessDays(
                        leaveDto.StartDate.Date,
                        leaveDto.EndDate.Date
                    ),
                    LeaveStatus = leaveDto.LeaveStatus, // Use the status from the form
                    Remarks = leaveDto.Remarks,
                };

                var createdLeave = await _leaveRepository.ApplyForLeaveAsync(entity);
                return Json(
                    new
                    {
                        success = true,
                        message = "Leave application submitted successfully.",
                        id = createdLeave.LeaveId,
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
                    new { success = false, message = $"Error applying for leave: {ex.Message}" }
                );
            }
        }

        // GET: /Leave/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var leave = await _leaveRepository.GetLeaveByIdAsync(id);
                if (leave == null)
                    return NotFound();

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
                    var errors = ModelState
                        .Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();
                    return BadRequest(new { success = false, message = string.Join("\n", errors) });
                }

                // Only status/remarks are updated according to repository method signature
                await _leaveRepository.UpdateLeaveStatusAsync(
                    leaveDto.LeaveId,
                    leaveDto.LeaveStatus ?? "Pending",
                    leaveDto.Remarks ?? string.Empty
                );
                return Json(
                    new
                    {
                        success = true,
                        message = "Leave updated successfully.",
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
                    new { success = false, message = $"Error updating leave: {ex.Message}" }
                );
            }
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
                    new { success = false, message = $"Error deleting leave: {ex.Message}" }
                );
            }
        }

        // GET: /Leave/CheckOverlap
        [HttpGet]
        public async Task<IActionResult> CheckOverlap(
            Guid employeeId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeLeaveId = null
        )
        {
            try
            {
                var hasOverlap = await _leaveRepository.HasLeaveOverlapAsync(
                    employeeId,
                    startDate.Date,
                    endDate.Date,
                    excludeLeaveId
                );
                return Json(new { success = true, hasOverlap });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET: /Leave/GetEmployeeName/{id}
        [HttpGet]
        public async Task<IActionResult> GetEmployeeName(Guid id)
        {
            try
            {
                var employee = await _context
                    .Employees.AsNoTracking()
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
            if (end < start)
                return 0;
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

        private LeaveDto MapToDto(Leave leave) =>
            new LeaveDto
            {
                LeaveId = leave.LeaveId,
                EmployeeId = leave.EmployeeId,
                LeaveType = leave.LeaveType,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                TotalDays = leave.TotalDays,
                LeaveStatus = leave.LeaveStatus,
                Remarks = leave.Remarks,
            };

        private async Task PopulateEmployeesAsync()
        {
            ViewBag.Employees = await _context
                .Employees.AsNoTracking()
                .OrderBy(e => e.EmployeeCode)
                .Select(e => new
                {
                    e.EmployeeId,
                    EmployeeCode = e.EmployeeCode ?? "N/A",
                    FullName = e.FullName ?? "Unknown",
                })
                .ToListAsync();
        }
    }
}

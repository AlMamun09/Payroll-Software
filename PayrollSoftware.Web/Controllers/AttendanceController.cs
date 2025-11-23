using System.Data;
using ExcelDataReader;
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
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AttendanceController(
            IAttendanceRepository attendanceRepository,
            ILeaveRepository leaveRepository,
            ApplicationDbContext context,
            IServiceScopeFactory serviceScopeFactory
        )
        {
            _attendanceRepository = attendanceRepository;
            _leaveRepository = leaveRepository;
            _context = context;
            _serviceScopeFactory = serviceScopeFactory;
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
                // 1. Get Base Data
                var attendances = await _attendanceRepository.GetAllAttendencesAsync();

                // 2. Get Query Parameters
                var statusFilter = (
                    Request.Query["statusFilter"].ToString() ?? string.Empty
                ).Trim();
                var dateFilter = Request.Query["dateFilter"].ToString();
                // NEW: Capture the employee filter
                var employeeFilter = (
                    Request.Query["employeeFilter"].ToString() ?? string.Empty
                ).Trim();

                // 3. Apply Basic Filters (Status & Date)
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    attendances = attendances
                        .Where(a =>
                            string.Equals(
                                a.Status,
                                statusFilter,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .ToList();
                }

                if (
                    !string.IsNullOrWhiteSpace(dateFilter)
                    && DateTime.TryParse(dateFilter, out var filterDate)
                )
                {
                    attendances = attendances
                        .Where(a => a.AttendanceDate.Date == filterDate.Date)
                        .ToList();
                }

                // 4. Prepare Lookups (Employees & Shifts)
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

                // 5. Project Data (Merge Lookup Info)
                var queryData = attendances.Select(a => new
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
                });

                // 6. NEW: Apply Employee Filter (Name or Code)
                // We filter AFTER projection because we need the joined EmployeeName/Code
                if (!string.IsNullOrWhiteSpace(employeeFilter))
                {
                    queryData = queryData.Where(x =>
                        (
                            x.EmployeeName != null
                            && x.EmployeeName.IndexOf(
                                employeeFilter,
                                StringComparison.OrdinalIgnoreCase
                            ) >= 0
                        )
                        || (
                            x.EmployeeCode != null
                            && x.EmployeeCode.IndexOf(
                                employeeFilter,
                                StringComparison.OrdinalIgnoreCase
                            ) >= 0
                        )
                    );
                }

                // 7. Return Result
                return Json(new { data = queryData.ToList() });
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(AttendanceImportDto model)
        {
            if (model.ExcelFile == null || model.ExcelFile.Length == 0)
                return Json(new { success = false, message = "Please select a file." });

            var results = new AttendanceImportResultDto();
            var rawPunches = new List<(Guid EmployeeId, DateTime Timestamp)>();

            // 1. CACHE: Map MachineCode (int) -> EmployeeId (Guid)
            // Only fetch active employees who have a MachineCode assigned
            var employeeLookup = await _context
                .Employees.AsNoTracking()
                .Where(e => e.Status == "Active" && e.MachineCode.HasValue)
                .ToDictionaryAsync(e => e.MachineCode.Value, e => e.EmployeeId);

            try
            {
                // Required for ExcelDataReader to work
                System.Text.Encoding.RegisterProvider(
                    System.Text.CodePagesEncodingProvider.Instance
                );

                using (var stream = model.ExcelFile.OpenReadStream())
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet();
                    var dataTable = result.Tables[0];

                    if (dataTable.Rows.Count < 2)
                        return Json(
                            new
                            {
                                success = false,
                                message = "Excel file is empty or missing headers.",
                            }
                        );

                    // 2. DYNAMIC HEADER MAPPING
                    // Scan first row to find column indexes for 'mid', 'date', 'time'
                    var headers = dataTable
                        .Rows[0]
                        .ItemArray.Select(x => x?.ToString()?.Trim().ToLower())
                        .ToList();

                    int midIndex = headers.IndexOf("mid");
                    int dateIndex = headers.IndexOf("date");
                    int timeIndex = headers.IndexOf("time");

                    if (midIndex == -1 || dateIndex == -1 || timeIndex == -1)
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = "Error: Excel must have columns named 'Mid', 'Date', and 'Time'.",
                            }
                        );
                    }

                    // 3. PARSE ROWS
                    for (int i = 1; i < dataTable.Rows.Count; i++)
                    {
                        var row = dataTable.Rows[i];

                        // Parse Mid (Machine ID)
                        string midString = row[midIndex].ToString()?.Trim();
                        if (!int.TryParse(midString, out int mid))
                            continue;

                        // Parse Date & Time
                        string dateString = row[dateIndex]?.ToString()?.Trim();
                        string timeString = row[timeIndex]?.ToString()?.Trim();

                        // Find Employee Guid using Mid
                        if (employeeLookup.TryGetValue(mid, out Guid realEmployeeId))
                        {
                            if (
                                DateTime.TryParse(dateString, out DateTime datePart)
                                && TimeSpan.TryParse(timeString, out TimeSpan timePart)
                            )
                            {
                                // Add punch to list
                                rawPunches.Add((realEmployeeId, datePart.Add(timePart)));
                            }
                        }
                    }
                }

                // 4. GROUP & CALCULATE (First-In / Last-Out)
                var groupedAttendance = rawPunches
                    .GroupBy(x => new { x.EmployeeId, Date = x.Timestamp.Date })
                    .Select(g => new
                    {
                        EmployeeId = g.Key.EmployeeId,
                        Date = g.Key.Date,
                        InTime = g.Min(x => x.Timestamp).TimeOfDay,
                        OutTime = g.Max(x => x.Timestamp).TimeOfDay,
                    })
                    .ToList();

                // 5. SAVE TO DATABASE
                foreach (var record in groupedAttendance)
                {
                    // Check if attendance already exists for this date
                    bool exists = await _context.Attendances.AnyAsync(a =>
                        a.EmployeeId == record.EmployeeId && a.AttendanceDate == record.Date
                    );

                    if (exists)
                        continue; // Skip duplicates

                    var attendance = new Attendance
                    {
                        AttendanceId = Guid.NewGuid(),
                        EmployeeId = record.EmployeeId,
                        AttendanceDate = record.Date,
                        InTime = record.InTime,
                        // If only 1 punch found (In==Out), set OutTime to null so it marks as Absent/Incomplete
                        OutTime = (record.InTime == record.OutTime) ? null : record.OutTime,
                        ShiftId = Guid.Empty, // Triggers repository to auto-assign shift
                    };

                    try
                    {
                        await _attendanceRepository.AddAttendanceAsync(attendance);
                        results.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        results.Errors.Add($"Date {record.Date:dd/MM}: {ex.Message}");
                        results.FailCount++;
                    }
                }

                return Json(
                    new
                    {
                        success = true,
                        message = $"Processed. Added: {results.SuccessCount}, Failed: {results.FailCount}",
                        errors = results.Errors,
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { success = false, message = "Server Error: " + ex.Message }
                );
            }
        }

        // 1. UPLOAD ENDPOINT (Starts the process)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImport(AttendanceImportDto model)
        {
            if (model.ExcelFile == null || model.ExcelFile.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            // A. Convert IFormFile to Byte Array
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await model.ExcelFile.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            // B. Save to DB (The "Backup" Step)
            var importFile = new AttendanceImportFile
            {
                ImportId = Guid.NewGuid(),
                FileName = model.ExcelFile.FileName,
                FileContent = fileBytes,
                Status = "Pending",
                TotalRows = 0,
                ProcessedRows = 0,
            };

            await _attendanceRepository.SaveImportFileAsync(importFile);

            // C. Start Background Processing (Fire and Forget)
            // We pass the ID to a separate method that runs in the background
            _ = Task.Run(() => ProcessImportBackground(importFile.ImportId));

            // D. Return ID to Frontend immediately
            return Json(new { success = true, importId = importFile.ImportId });
        }

        // 2. POLLING ENDPOINT (Frontend calls this every 1s)
        [HttpGet]
        public async Task<IActionResult> CheckProgress(Guid importId)
        {
            var file = await _attendanceRepository.GetImportStatusAsync(importId);
            if (file == null)
                return NotFound();

            // Calculate Percentage
            int pct = 0;
            if (file.TotalRows > 0)
                pct = (int)((double)file.ProcessedRows / file.TotalRows * 100);

            // Cap at 100 if completed
            if (file.Status == "Completed")
                pct = 100;

            return Json(
                new
                {
                    status = file.Status,
                    percentage = pct,
                    processed = file.ProcessedRows,
                    total = file.TotalRows,
                    errors = file.ErrorLog,
                }
            );
        }

        // 3. BACKGROUND WORKER (Private)

        private async Task ProcessImportBackground(Guid importId)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var importFile = await repo.GetImportStatusAsync(importId);
                if (importFile == null)
                    return;

                await repo.UpdateImportProgressAsync(importId, 0, 0, "Processing");

                var errors = new List<string>();
                var rawPunches = new List<(Guid EmployeeId, DateTime Timestamp)>();
                var debugInfo = new List<string>(); // ADD: Debug tracking

                try
                {
                    System.Text.Encoding.RegisterProvider(
                        System.Text.CodePagesEncodingProvider.Instance
                    );

                    using (var stream = new MemoryStream(importFile.FileContent))
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet();
                        var dataTable = result.Tables[0];
                        int totalRows = dataTable.Rows.Count - 1;

                        await repo.UpdateImportProgressAsync(importId, 0, totalRows, "Processing");

                        // Headers
                        var headers = dataTable
                            .Rows[0]
                            .ItemArray.Select(x => x?.ToString()?.Trim().ToLower())
                            .ToList();

                        int midIndex = headers.IndexOf("mid");
                        int dateIndex = headers.IndexOf("date");
                        int timeIndex = headers.IndexOf("time");

                        if (midIndex == -1 || dateIndex == -1 || timeIndex == -1)
                        {
                            throw new Exception("Missing required columns: Mid, Date, Time");
                        }

                        // **FIX 1**: Load ALL employees with MachineCode (not just Active)
                        // This ensures we can see ALL machine codes that exist
                        var allEmployeesWithMachineCode = await context
                            .Employees.AsNoTracking()
                            .Where(e => e.MachineCode.HasValue)
                            .Select(e => new
                            {
                                e.MachineCode,
                                e.EmployeeId,
                                e.Status,
                                e.FullName,
                            })
                            .ToListAsync();

                        // **FIX 2**: Only use Active employees for actual import
                        var employeeLookup = allEmployeesWithMachineCode
                            .Where(e => e.Status == "Active")
                            .ToDictionary(e => e.MachineCode!.Value, e => e.EmployeeId);

                        // **DEBUG**: Log available machine codes
                        debugInfo.Add(
                            $"Total employees with MachineCode: {allEmployeesWithMachineCode.Count}"
                        );
                        debugInfo.Add($"Active employees: {employeeLookup.Count}");
                        debugInfo.Add(
                            $"Machine Codes: {string.Join(", ", employeeLookup.Keys.OrderBy(x => x))}"
                        );

                        var invalidMids = new HashSet<int>(); // Track invalid MIDs

                        for (int i = 1; i < dataTable.Rows.Count; i++)
                        {
                            var row = dataTable.Rows[i];
                            string midString = row[midIndex]?.ToString()?.Trim();

                            if (int.TryParse(midString, out int mid))
                            {
                                DateTime datePart = DateTime.MinValue;
                                TimeSpan timePart = TimeSpan.Zero;
                                bool isDateValid = false;
                                bool isTimeValid = false;

                                // --- ROBUST DATE PARSING (IMPROVED) ---
                                var dateCell = row[dateIndex];

                                if (dateCell is DateTime dt)
                                {
                                    datePart = dt.Date; // **FIX**: Ensure date only
                                    isDateValid = true;
                                }
                                else if (dateCell is double serialDate)
                                {
                                    try
                                    {
                                        datePart = DateTime.FromOADate(serialDate).Date; // **FIX**: Date only
                                        isDateValid = true;
                                    }
                                    catch { }
                                }
                                else if (dateCell != null)
                                {
                                    string dateStr = dateCell.ToString().Trim();

                                    // **FIX 3**: Add more date formats including d/M/yyyy
                                    string[] formats =
                                    {
                                        "yyyy-MM-dd",
                                        "dd/MM/yyyy",
                                        "d/M/yyyy", // Single digit day/month
                                        "MM/dd/yyyy",
                                        "M/d/yyyy", // Single digit month/day
                                        "yyyy/MM/dd",
                                        "dd-MM-yyyy",
                                        "d-M-yyyy",
                                        "MM-dd-yyyy",
                                        "M-d-yyyy",
                                    };

                                    if (
                                        DateTime.TryParseExact(
                                            dateStr,
                                            formats,
                                            System.Globalization.CultureInfo.InvariantCulture,
                                            System.Globalization.DateTimeStyles.None,
                                            out var parsedDate
                                        )
                                    )
                                    {
                                        datePart = parsedDate.Date; // **FIX**: Date only
                                        isDateValid = true;
                                    }
                                    // Fallback to general parsing if specific formats fail
                                    else if (DateTime.TryParse(dateStr, out parsedDate))
                                    {
                                        datePart = parsedDate.Date; // **FIX**: Date only
                                        isDateValid = true;
                                    }
                                }

                                // --- ROBUST TIME PARSING (same as before) ---
                                var timeCell = row[timeIndex];
                                if (timeCell is DateTime dtTime)
                                {
                                    timePart = dtTime.TimeOfDay;
                                    isTimeValid = true;
                                }
                                else if (timeCell is TimeSpan ts)
                                {
                                    timePart = ts;
                                    isTimeValid = true;
                                }
                                else if (timeCell is double serialTime)
                                {
                                    try
                                    {
                                        timePart = DateTime.FromOADate(serialTime).TimeOfDay;
                                        isTimeValid = true;
                                    }
                                    catch { }
                                }
                                else if (timeCell != null)
                                {
                                    string timeStr = timeCell.ToString().Trim();
                                    if (TimeSpan.TryParse(timeStr, out var parsedTime))
                                    {
                                        timePart = parsedTime;
                                        isTimeValid = true;
                                    }
                                    else if (DateTime.TryParse(timeStr, out var parsedDateTime))
                                    {
                                        timePart = parsedDateTime.TimeOfDay;
                                        isTimeValid = true;
                                    }
                                }

                                // --- COMBINE AND SAVE ---
                                if (isDateValid && isTimeValid)
                                {
                                    if (employeeLookup.TryGetValue(mid, out Guid realEmployeeId))
                                    {
                                        rawPunches.Add(
                                            (realEmployeeId, datePart.Date.Add(timePart))
                                        );
                                    }
                                    else
                                    {
                                        // **DEBUG**: Track invalid MIDs
                                        if (invalidMids.Add(mid))
                                        {
                                            // Check if MID exists but employee is inactive
                                            var inactiveEmp =
                                                allEmployeesWithMachineCode.FirstOrDefault(e =>
                                                    e.MachineCode == mid
                                                );

                                            if (inactiveEmp != null)
                                            {
                                                debugInfo.Add(
                                                    $"MID {mid} belongs to {inactiveEmp.Status} employee: {inactiveEmp.FullName}"
                                                );
                                            }
                                            else
                                            {
                                                debugInfo.Add($"MID {mid} not found in system");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // **DEBUG**: Track date/time parsing failures
                                    if (!isDateValid)
                                        debugInfo.Add($"Row {i}: Invalid date format - {dateCell}");
                                    if (!isTimeValid)
                                        debugInfo.Add($"Row {i}: Invalid time format - {timeCell}");
                                }
                            }

                            if (i % 50 == 0)
                                await repo.UpdateImportProgressAsync(
                                    importId,
                                    i,
                                    totalRows,
                                    "Processing"
                                );
                        }
                    }

                    // --- SAVE TO DB ---
                    var grouped = rawPunches
                        .GroupBy(x => new { x.EmployeeId, Date = x.Timestamp.Date })
                        .ToList();
                    int savedCount = 0;
                    int totalGroups = grouped.Count;

                    await repo.UpdateImportProgressAsync(importId, 0, totalGroups, "Saving...");

                    if (totalGroups == 0)
                    {
                        // **FIX 4**: Include debug info in error message
                        var errorMessage =
                            "No valid records found. Check Date Formats and Machine IDs.\n\n"
                            + "Debug Info:\n"
                            + string.Join("\n", debugInfo.Take(20));

                        await repo.UpdateImportProgressAsync(
                            importId,
                            0,
                            0,
                            "Completed",
                            errorMessage
                        );
                        return;
                    }

                    foreach (var record in grouped)
                    {
                        bool exists = await context.Attendances.AnyAsync(a =>
                            a.EmployeeId == record.Key.EmployeeId
                            && a.AttendanceDate == record.Key.Date
                        );

                        if (!exists)
                        {
                            var attendance = new Attendance
                            {
                                AttendanceId = Guid.NewGuid(),
                                EmployeeId = record.Key.EmployeeId,
                                AttendanceDate = record.Key.Date,
                                InTime = record.Min(x => x.Timestamp).TimeOfDay,
                                OutTime = record.Max(x => x.Timestamp).TimeOfDay,
                                ShiftId = Guid.Empty,
                            };

                            if (attendance.InTime == attendance.OutTime)
                                attendance.OutTime = null;

                            try
                            {
                                await repo.AddAttendanceAsync(attendance);
                            }
                            catch (Exception ex)
                            {
                                errors.Add(
                                    $"Emp {record.Key.EmployeeId} on {record.Key.Date:dd/MM}: {ex.Message}"
                                );
                            }
                        }

                        savedCount++;
                        if (savedCount % 10 == 0)
                            await repo.UpdateImportProgressAsync(
                                importId,
                                savedCount,
                                totalGroups,
                                "Saving..."
                            );
                    }

                    // **FIX 5**: Add debug summary to completion message
                    var completionMessage =
                        errors.Count > 0
                            ? string.Join(" | ", errors.Take(5))
                            : $"Import successful. Processed {totalGroups} attendance records.";

                    await repo.UpdateImportProgressAsync(
                        importId,
                        totalGroups,
                        totalGroups,
                        "Completed",
                        completionMessage
                    );
                }
                catch (Exception ex)
                {
                    var errorMsg = ex.Message;
                    if (debugInfo.Any())
                    {
                        errorMsg += "\n\nDebug Info:\n" + string.Join("\n", debugInfo.Take(10));
                    }
                    await repo.UpdateImportProgressAsync(importId, 0, 0, "Failed", errorMsg);
                }
            }
        }
    }
}

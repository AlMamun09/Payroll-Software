using System.Data;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;
using PayrollSoftware.Infrastructure.Services.Interfaces;

namespace PayrollSoftware.Infrastructure.Services
{
    public class AttendanceImportService : IAttendanceImportService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ILookupRepository _lookupRepository;

        public AttendanceImportService(
            IServiceScopeFactory serviceScopeFactory,
            IAttendanceRepository attendanceRepository,
            ILookupRepository lookupRepository)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _attendanceRepository = attendanceRepository;
            _lookupRepository = lookupRepository;
        }

        public async Task<(bool Success, Guid? ImportId, string Message)> UploadImportAsync(AttendanceImportDto model)
        {
            if (model.ExcelFile == null || model.ExcelFile.Length == 0)
                return (false, null, "No file selected.");

            try
            {
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    await model.ExcelFile.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }

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

                _ = Task.Run(() => ProcessImportBackground(importFile.ImportId));

                return (true, importFile.ImportId, "Import started successfully.");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error starting import: {ex.Message}");
            }
        }

        public async Task<(string Status, int Percentage, int Processed, int Total, string? Errors)> CheckProgressAsync(Guid importId)
        {
            var file = await _attendanceRepository.GetImportStatusAsync(importId);
            if (file == null)
                return ("NotFound", 0, 0, 0, null);

            int percentage = 0;
            if (file.TotalRows > 0)
                percentage = (int)((double)file.ProcessedRows / file.TotalRows * 100);

            if (file.Status == "Completed")
                percentage = 100;

            return (file.Status, percentage, file.ProcessedRows, file.TotalRows, file.ErrorLog);
        }

        private async Task ProcessImportBackground(Guid importId)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var lookupRepo = scope.ServiceProvider.GetRequiredService<ILookupRepository>();

                var importFile = await repo.GetImportStatusAsync(importId);
                if (importFile == null)
                    return;

                await repo.UpdateImportProgressAsync(importId, 0, 0, "Processing");

                var errors = new List<string>();
                var rawPunches = new List<(Guid EmployeeId, DateTime Timestamp)>();
                var debugInfo = new List<string>();

                try
                {
                    // Fetch weekend days from Lookup table
                    var weekendLookups = await lookupRepo.GetLookupsByTypeAsync("Weekend");
                    var weekendDays = weekendLookups
                        .Where(l => l.IsActive)
                        .Select(l => l.LookupValue)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    debugInfo.Add($"Weekend Days Configured: {string.Join(", ", weekendDays)}");

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

                        var employeeLookup = allEmployeesWithMachineCode
                            .Where(e => e.Status == "Active")
                            .ToDictionary(e => e.MachineCode!.Value, e => e.EmployeeId);

                        debugInfo.Add(
                            $"Total employees with MachineCode: {allEmployeesWithMachineCode.Count}"
                        );
                        debugInfo.Add($"Active employees: {employeeLookup.Count}");
                        debugInfo.Add(
                            $"Machine Codes: {string.Join(", ", employeeLookup.Keys.OrderBy(x => x))}"
                        );

                        var invalidMids = new HashSet<int>();
                        int weekendSkipCount = 0;

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

                                var dateCell = row[dateIndex];

                                if (dateCell is DateTime dt)
                                {
                                    datePart = dt.Date;
                                    isDateValid = true;
                                }
                                else if (dateCell is double serialDate)
                                {
                                    try
                                    {
                                        datePart = DateTime.FromOADate(serialDate).Date;
                                        isDateValid = true;
                                    }
                                    catch { }
                                }
                                else if (dateCell != null)
                                {
                                    string dateStr = dateCell.ToString().Trim();

                                    string[] formats =
                                    {
                                        "yyyy-MM-dd",
                                        "dd/MM/yyyy",
                                        "d/M/yyyy",
                                        "MM/dd/yyyy",
                                        "M/d/yyyy",
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
                                        datePart = parsedDate.Date;
                                        isDateValid = true;
                                    }
                                    else if (DateTime.TryParse(dateStr, out parsedDate))
                                    {
                                        datePart = parsedDate.Date;
                                        isDateValid = true;
                                    }
                                }

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

                                if (isDateValid && isTimeValid)
                                {
                                    // WEEKEND VALIDATION: Skip if the date falls on a weekend
                                    string dayOfWeek = datePart.DayOfWeek.ToString();
                                    if (weekendDays.Contains(dayOfWeek))
                                    {
                                        weekendSkipCount++;
                                        continue; // Skip this row
                                    }

                                    if (employeeLookup.TryGetValue(mid, out Guid realEmployeeId))
                                    {
                                        rawPunches.Add(
                                            (realEmployeeId, datePart.Date.Add(timePart))
                                        );
                                    }
                                    else
                                    {
                                        if (invalidMids.Add(mid))
                                        {
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

                        if (weekendSkipCount > 0)
                        {
                            debugInfo.Add($"Skipped {weekendSkipCount} weekend records");
                        }
                    }

                    var grouped = rawPunches
                        .GroupBy(x => new { x.EmployeeId, Date = x.Timestamp.Date })
                        .ToList();
                    int savedCount = 0;
                    int totalGroups = grouped.Count;

                    await repo.UpdateImportProgressAsync(importId, 0, totalGroups, "Saving...");

                    if (totalGroups == 0)
                    {
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

                    var completionMessage =
                        errors.Count > 0
                            ? string.Join(" | ", errors.Take(5))
                            : $"Import successful. Processed {totalGroups} attendance records.";

                    if (debugInfo.Any())
                    {
                        completionMessage += "\n\nDebug Info:\n" + string.Join("\n", debugInfo.Take(10));
                    }

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

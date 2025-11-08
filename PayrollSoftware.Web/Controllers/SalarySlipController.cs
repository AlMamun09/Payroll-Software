using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;

namespace PayrollSoftware.Web.Controllers
{
    public class SalarySlipController : Controller
    {
        private readonly ISalarySlipRepository _salarySlipRepository;
        private readonly IPayrollRepository _payrollRepository;
        private readonly ApplicationDbContext _context;

        public SalarySlipController(
            ISalarySlipRepository salarySlipRepository,
            IPayrollRepository payrollRepository,
            ApplicationDbContext context
        )
        {
            _salarySlipRepository = salarySlipRepository;
            _payrollRepository = payrollRepository;
            _context = context;
        }

        // GET: SalarySlip/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET: SalarySlip/GetSalarySlipsJson
        [HttpGet]
        public async Task<IActionResult> GetSalarySlipsJson()
        {
            try
            {
                var salarySlips = await _salarySlipRepository.GetAllSalarySlipsAsync();

                // Get employee details
                var employeeIds = salarySlips.Select(s => s.EmployeeId).Distinct().ToList();
                var employees = await _context
                    .Employees.AsNoTracking()
                    .Where(e => employeeIds.Contains(e.EmployeeId))
                    .ToDictionaryAsync(e => e.EmployeeId, e => new { e.EmployeeCode, e.FullName });

                // Get payroll details
                var payrollIds = salarySlips.Select(s => s.PayrollId).Distinct().ToList();
                var payrolls = await _context
                    .Payrolls.AsNoTracking()
                    .Where(p => payrollIds.Contains(p.PayrollId))
                    .ToDictionaryAsync(
                        p => p.PayrollId,
                        p => new
                        {
                            p.PayPeriodStart,
                            p.PayPeriodEnd,
                            p.PaymentStatus,
                        }
                    );

                var dtos = salarySlips
                    .Select(s => new
                    {
                        s.SalarySlipId,
                        s.PayrollId,
                        s.EmployeeId,
                        EmployeeCode = employees.TryGetValue(s.EmployeeId, out var emp)
                            ? emp.EmployeeCode
                            : "N/A",
                        EmployeeName = employees.TryGetValue(s.EmployeeId, out var emp2)
                            ? emp2.FullName
                            : "Unknown",
                        s.Month,
                        s.Year,
                        MonthYear = $"{GetMonthName(s.Month)} {s.Year}",
                        PayPeriodStart = payrolls.TryGetValue(s.PayrollId, out var p)
                            ? p.PayPeriodStart
                            : DateTime.MinValue,
                        PayPeriodEnd = payrolls.TryGetValue(s.PayrollId, out var p2)
                            ? p2.PayPeriodEnd
                            : DateTime.MinValue,
                        s.GrossEarnings,
                        s.TotalDeductions,
                        s.NetPay,
                        s.GeneratedDate,
                        PaymentStatus = payrolls.TryGetValue(s.PayrollId, out var p3)
                            ? p3.PaymentStatus
                            : "Unknown",
                    })
                    .ToList();

                return Json(new { data = dtos });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: SalarySlip/View/{id}
        [HttpGet]
        public async Task<IActionResult> View(Guid id)
        {
            try
            {
                var salarySlip = await _salarySlipRepository.GetSalarySlipByIdAsync(id);
                if (salarySlip == null)
                {
                    TempData["Error"] = "Salary slip not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Redirect to the payroll salary slip view
                return RedirectToAction(
                    "ViewSalarySlip",
                    "Payroll",
                    new { id = salarySlip.PayrollId }
                );
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading salary slip: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SalarySlip/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var salarySlip = await _salarySlipRepository.GetSalarySlipByIdAsync(id);
                if (salarySlip == null)
                {
                    return NotFound(new { success = false, message = "Salary slip not found." });
                }

                // Check if associated payroll exists and update its status
                var payroll = await _payrollRepository.GetPayrollByIdAsync(salarySlip.PayrollId);
                if (payroll != null)
                {
                    payroll.PaymentStatus = "Pending";
                    payroll.PaymentDate = null;
                    await _payrollRepository.UpdatePayrollAsync(payroll);
                }

                await _salarySlipRepository.DeleteSalarySlipAsync(id);
                return Json(new { success = true, message = "Salary slip deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Helper method to get month name
        private string GetMonthName(int month)
        {
            return month switch
            {
                1 => "January",
                2 => "February",
                3 => "March",
                4 => "April",
                5 => "May",
                6 => "June",
                7 => "July",
                8 => "August",
                9 => "September",
                10 => "October",
                11 => "November",
                12 => "December",
                _ => "Unknown",
            };
        }
    }
}

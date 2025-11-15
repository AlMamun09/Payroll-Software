using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;

namespace PayrollSoftware.Web.Controllers
{
    public class PayrollController : Controller
    {
        private readonly IPayrollRepository _payrollRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ISalarySlipRepository _salarySlipRepository;
        private readonly ApplicationDbContext _context;

        public PayrollController(
            IPayrollRepository payrollRepository,
            IEmployeeRepository employeeRepository,
            ISalarySlipRepository salarySlipRepository,
            ApplicationDbContext context
        )
        {
            _payrollRepository = payrollRepository;
            _employeeRepository = employeeRepository;
            _salarySlipRepository = salarySlipRepository;
            _context = context;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetPayrollsJson()
        {
            try
            {
                var payrolls = await _payrollRepository.GetAllPayrollsAsync();
                // Include BasicSalary from Employee table (original), not the pro-rated stored in payroll record
                var empLookup = await _context
                    .Employees.AsNoTracking()
                    .ToDictionaryAsync(
                        e => e.EmployeeId,
                        e => new
                        {
                            e.EmployeeCode,
                            e.FullName,
                            e.BasicSalary,
                        }
                    );

                var dtos = payrolls
                    .Select(p => new
                    {
                        p.PayrollId,
                        p.EmployeeId,
                        EmployeeCode = empLookup.TryGetValue(p.EmployeeId, out var emp)
                            ? emp.EmployeeCode
                            : "N/A",
                        EmployeeName = empLookup.TryGetValue(p.EmployeeId, out var emp2)
                            ? emp2.FullName
                            : "Unknown",
                        p.PayPeriodStart,
                        p.PayPeriodEnd,
                        p.TotalDays,
                        p.PresentDays,
                        p.PaidLeaveDays,
                        p.UnpaidLeaveDays,
                        p.AbsentDays,
                        p.PayableDays,
                        BasicSalary = empLookup.TryGetValue(p.EmployeeId, out var emp3)
                            ? emp3.BasicSalary
                            : 0m,
                        p.TotalAllowances,
                        p.TotalDeductions,
                        p.NetSalary,
                        p.PaymentStatus,
                        p.PaymentDate,
                    })
                    .ToList();
                return Json(new { data = dtos });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(
            Guid employeeId,
            DateTime periodStart,
            DateTime periodEnd
        )
        {
            try
            {
                if (employeeId == Guid.Empty)
                    return BadRequest(new { success = false, message = "Employee is required." });
                if (periodEnd < periodStart)
                    return BadRequest(
                        new { success = false, message = "End date cannot be before start date." }
                    );

                var payroll = await _payrollRepository.ProcessPayrollAsync(
                    employeeId,
                    periodStart.Date,
                    periodEnd.Date
                );
                return Json(
                    new
                    {
                        success = true,
                        message = "Payroll processed successfully.",
                        id = payroll.PayrollId,
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
                    new { success = false, message = $"Error processing payroll: {ex.Message}" }
                );
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(Guid id)
        {
            try
            {
                var payroll = await _payrollRepository.GetPayrollByIdAsync(id);
                if (payroll == null)
                    return NotFound(new { success = false, message = "Payroll not found." });

                // Mark as paid
                payroll.PaymentStatus = "Paid";
                payroll.PaymentDate = DateTime.UtcNow;
                await _payrollRepository.UpdatePayrollAsync(payroll);

                // Generate salary slip
                var existingSlip = await _salarySlipRepository.GetSalarySlipByPayrollIdAsync(
                    payroll.PayrollId
                );
                if (existingSlip == null)
                {
                    var salarySlip = new Infrastructure.Domain.Entities.SalarySlip
                    {
                        SalarySlipId = Guid.NewGuid(),
                        PayrollId = payroll.PayrollId,
                        EmployeeId = payroll.EmployeeId,
                        Month = payroll.PayPeriodStart.Month,
                        Year = payroll.PayPeriodStart.Year,
                        GrossEarnings = payroll.BasicSalary + payroll.TotalAllowances,
                        TotalDeductions = payroll.TotalDeductions,
                        NetPay = payroll.NetSalary,
                        GeneratedDate = DateTime.UtcNow,
                    };
                    await _salarySlipRepository.CreateSalarySlipAsync(salarySlip);
                }

                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(
                        new
                        {
                            success = true,
                            message = "Payroll marked as paid and salary slip generated.",
                            redirectUrl = Url.Action(
                                "ViewSalarySlip",
                                new { id = payroll.PayrollId }
                            ),
                        }
                    );
                }

                // For form post from details page - redirect to salary slip
                return RedirectToAction("ViewSalarySlip", new { id = payroll.PayrollId });
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return StatusCode(500, new { success = false, message = ex.Message });
                }

                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _payrollRepository.DeletePayrollAsync(id);
                return Json(new { success = true, message = "Payroll deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var payroll = await _payrollRepository.GetPayrollByIdAsync(id);
                if (payroll == null)
                {
                    return NotFound();
                }

                // Get employee details
                var employee = await _context
                    .Employees.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeId == payroll.EmployeeId);

                if (employee != null)
                {
                    ViewBag.EmployeeCode = employee.EmployeeCode;
                    ViewBag.EmployeeName = employee.FullName;
                    ViewBag.Department = employee.Department;
                    ViewBag.Designation = employee.Designation;
                }

                // Get allowances and deductions applied during this period
                var allowancesDeductions = await _context
                    .AllowanceDeductions.AsNoTracking()
                    .Where(ad =>
                        ad.IsActive
                        && ad.EffectiveFrom.Date <= payroll.PayPeriodEnd.Date
                        && (
                            ad.EffectiveTo == null
                            || ad.EffectiveTo.Value.Date >= payroll.PayPeriodStart.Date
                        )
                        && (ad.IsCompanyWide || ad.EmployeeId == payroll.EmployeeId)
                    )
                    .ToListAsync();

                ViewBag.Allowances = allowancesDeductions
                    .Where(ad => ad.AllowanceDeductionType == "Allowance")
                    .ToList();

                ViewBag.Deductions = allowancesDeductions
                    .Where(ad => ad.AllowanceDeductionType == "Deduction")
                    .ToList();

                ViewBag.AllowancesDeductions = allowancesDeductions;

                return View(payroll);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading payroll details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewSalarySlip(Guid id)
        {
            try
            {
                var payroll = await _payrollRepository.GetPayrollByIdAsync(id);
                if (payroll == null)
                {
                    return NotFound();
                }

                // Get employee details
                var employee = await _context
                    .Employees.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeId == payroll.EmployeeId);

                if (employee == null)
                {
                    return NotFound();
                }

                ViewBag.EmployeeCode = employee.EmployeeCode;
                ViewBag.EmployeeName = employee.FullName;
                ViewBag.MobileNumber = employee.MobileNumber;
                ViewBag.AccountNumber = employee.BankAccountNumber;
                ViewBag.BankName = employee.BankAndBranchName;
                ViewBag.Department = employee.Department;
                ViewBag.Designation = employee.Designation;

                // Get allowances and deductions applied during this period
                var allowancesDeductions = await _context
                    .AllowanceDeductions.AsNoTracking()
                    .Where(ad =>
                        ad.IsActive
                        && ad.EffectiveFrom.Date <= payroll.PayPeriodEnd.Date
                        && (
                            ad.EffectiveTo == null
                            || ad.EffectiveTo.Value.Date >= payroll.PayPeriodStart.Date
                        )
                        && (ad.IsCompanyWide || ad.EmployeeId == payroll.EmployeeId)
                    )
                    .ToListAsync();

                ViewBag.Allowances = allowancesDeductions
                    .Where(ad => ad.AllowanceDeductionType == "Allowance")
                    .ToList();

                ViewBag.Deductions = allowancesDeductions
                    .Where(ad => ad.AllowanceDeductionType == "Deduction")
                    .ToList();

                // Calculate pro-rated salary
                var proRatedSalary =
                    payroll.TotalDays > 0
                        ? Math.Round(
                            (employee.BasicSalary / payroll.TotalDays) * payroll.PayableDays,
                            2
                        )
                        : 0;
                ViewBag.ProRatedSalary = proRatedSalary;

                return View(payroll);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading salary slip: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

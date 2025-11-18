using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Models;

namespace PayrollSoftware.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IPayrollRepository _payrollRepository;

        public HomeController(
            ILogger<HomeController> logger,
            IEmployeeRepository employeeRepository,
            IAttendanceRepository attendanceRepository,
            IPayrollRepository payrollRepository
        )
        {
            _logger = logger;
            _employeeRepository = employeeRepository;
            _attendanceRepository = attendanceRepository;
            _payrollRepository = payrollRepository;
        }

        public async Task<IActionResult> Index()
        {
            var employees = await _employeeRepository.GetAllEmployeesAsync();
            var attendances = await _attendanceRepository.GetAllAttendencesAsync();
            var payrolls = await _payrollRepository.GetAllPayrollsAsync();

            var today = DateTime.Today;
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var todayAttendances = attendances.Where(a => a.AttendanceDate.Date == today).ToList();

            var thisMonthPayrolls = payrolls
                .Where(p =>
                    p.PayPeriodEnd.Month == currentMonth && p.PayPeriodEnd.Year == currentYear
                )
                .ToList();

            var dashboardData = new DashboardDto
            {
                TotalEmployees = employees.Count,
                ActiveEmployees = employees.Count(e =>
                    e.Status?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true
                ),
                InactiveEmployees = employees.Count(e =>
                    e.Status?.Equals("Inactive", StringComparison.OrdinalIgnoreCase) == true
                ),
                TotalPresentToday = todayAttendances.Count(a =>
                    a.Status?.Equals("Present", StringComparison.OrdinalIgnoreCase) == true
                ),
                TotalAbsentToday = todayAttendances.Count(a =>
                    a.Status?.Equals("Absent", StringComparison.OrdinalIgnoreCase) == true
                ),
                TotalLateToday = todayAttendances.Count(a =>
                    a.LateEntry != null && a.LateEntry > TimeSpan.Zero
                ),

                // Payroll Statistics
                TotalPayrollsThisMonth = thisMonthPayrolls.Count,
                PendingPayments = thisMonthPayrolls.Count(p =>
                    p.PaymentStatus?.Equals("Pending", StringComparison.OrdinalIgnoreCase) == true
                ),
                CompletedPayments = thisMonthPayrolls.Count(p =>
                    p.PaymentStatus?.Equals("Paid", StringComparison.OrdinalIgnoreCase) == true
                ),
                TotalPayrollAmountThisMonth = thisMonthPayrolls.Sum(p => p.NetSalary),
            };

            return View(dashboardData);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                }
            );
        }
    }
}

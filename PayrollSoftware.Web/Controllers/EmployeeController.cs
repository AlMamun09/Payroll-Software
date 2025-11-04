using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Domain.Entities;
using PayrollSoftware.Infrastructure.Application.Interfaces;

namespace PayrollSoftware.Web.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ApplicationDbContext _context;

        public EmployeeController(IEmployeeRepository employeeRepository, ApplicationDbContext context)
        {
            _employeeRepository = employeeRepository;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeesJson()
        {
            try
            {
                // Load related names for listing
                var deptLookup = await _context.Departments.AsNoTracking().ToDictionaryAsync(d => d.DepartmentId, d => d.DepartmentName);
                var desigLookup = await _context.Designations.AsNoTracking().ToDictionaryAsync(d => d.DesignationId, d => d.DesignationName);

                var employees = await _employeeRepository.GetAllEmployeesAsync();
                var dtos = employees.Select(e => new EmployeeDto
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeNumericId = e.EmployeeNumericId,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Gender = e.Gender,
                    DateOfBirth = e.DateOfBirth,
                    JoiningDate = e.JoiningDate,
                    BasicSalary = e.BasicSalary,
                    EmploymentType = e.EmploymentType,
                    PaymentSystem = e.PaymentSystem,
                    AccountHolderName = e.AccountHolderName,
                    BankAndBranchName = e.BankAndBranchName,
                    BankAccountNumber = e.BankAccountNumber,
                    MobileNumber = e.MobileNumber,
                    Status = e.Status,
                    DepartmentId = e.DepartmentId,
                    DesignationId = e.DesignationId,
                    ShiftId = e.ShiftId,
                    DepartmentName = deptLookup.TryGetValue(e.DepartmentId, out var dn) ? dn : null,
                    DesignationName = desigLookup.TryGetValue(e.DesignationId, out var en) ? en : null
                }).ToList();

                return Json(new { data = dtos });
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            ViewBag.Title = "Create Employee";
            ViewBag.FormAction = Url.Action("Create");
            return View("Create", new EmployeeDto { DateOfBirth = DateTime.Today, JoiningDate = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] EmployeeDto dto)
        {
            try
            {
                var entity = MapToEntity(dto);
                entity.EmployeeId = Guid.NewGuid();
                await _employeeRepository.AddEmployeeAsync(entity);
                return Json(new { success = true, message = "Employee created successfully." });
            }
            catch (ArgumentException ex)
            {
                // Validation errors from repository
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Business rule violations (e.g., duplicate employee code)
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // Unexpected errors
                return StatusCode(500, new { success = false, message = $"Error creating employee: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var entity = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync();
            var dto = MapToDto(entity);
            ViewBag.Title = "Edit Employee";
            ViewBag.FormAction = Url.Action("Edit");
            return View("Create", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] EmployeeDto dto)
        {
            try
            {
                var entity = MapToEntity(dto);
                await _employeeRepository.UpdateEmployeeAsync(entity);
                return Json(new { success = true, message = "Employee updated successfully." });
            }
            catch (ArgumentException ex)
            {
                // Validation errors from repository
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Business rule violations
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // Unexpected errors
                return StatusCode(500, new { success = false, message = $"Error updating employee: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var e = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (e == null) return NotFound();

            var dept = await _context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.DepartmentId == e.DepartmentId);
            var desig = await _context.Designations.AsNoTracking().FirstOrDefaultAsync(d => d.DesignationId == e.DesignationId);
            var shift = e.ShiftId.HasValue
                ? await _context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.ShiftId == e.ShiftId.Value)
                : null;

            var dto = MapToDto(e);
            dto.DepartmentName = dept?.DepartmentName;
            dto.DesignationName = desig?.DesignationName;
            dto.ShiftName = shift?.ShiftName;
            return View("Details", dto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            await _employeeRepository.DeleteEmployeeAsync(id);
            return Json(new { success = true, message = "Employee deleted successfully." });
        }

        private Employee MapToEntity(EmployeeDto dto)
        {
            return new Employee
            {
                EmployeeId = dto.EmployeeId,
                DesignationId = dto.DesignationId,
                DepartmentId = dto.DepartmentId,
                ShiftId = dto.ShiftId,
                EmployeeNumericId = dto.EmployeeNumericId,
                EmployeeCode = dto.EmployeeCode,
                FullName = dto.FullName,
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth,
                JoiningDate = dto.JoiningDate,
                BasicSalary = dto.BasicSalary,
                EmploymentType = dto.EmploymentType,
                PaymentSystem = dto.PaymentSystem,
                AccountHolderName = dto.AccountHolderName,
                BankAndBranchName = dto.BankAndBranchName,
                BankAccountNumber = dto.BankAccountNumber,
                MobileNumber = dto.MobileNumber,
                Status = dto.Status
            };
        }

        private EmployeeDto MapToDto(Employee entity)
        {
            return new EmployeeDto
            {
                EmployeeId = entity.EmployeeId,
                DesignationId = entity.DesignationId,
                DepartmentId = entity.DepartmentId,
                ShiftId = entity.ShiftId,
                EmployeeNumericId = entity.EmployeeNumericId,
                EmployeeCode = entity.EmployeeCode,
                FullName = entity.FullName,
                Gender = entity.Gender,
                DateOfBirth = entity.DateOfBirth,
                JoiningDate = entity.JoiningDate,
                BasicSalary = entity.BasicSalary,
                EmploymentType = entity.EmploymentType,
                PaymentSystem = entity.PaymentSystem,
                AccountHolderName = entity.AccountHolderName,
                BankAndBranchName = entity.BankAndBranchName,
                BankAccountNumber = entity.BankAccountNumber,
                MobileNumber = entity.MobileNumber,
                Status = entity.Status
            };
        }

        private async Task PopulateDropdownsAsync()
        {
            ViewBag.Departments = await _context.Departments
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            ViewBag.Designations = await _context.Designations
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DesignationName)
                .ToListAsync();

            ViewBag.Shifts = await _context.Shifts
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.ShiftName)
                .ToListAsync();
        }

    }
}

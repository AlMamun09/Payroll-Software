using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Web.Controllers
{
    public class AllowanceDeductionController : Controller
    {
        private readonly IAllowanceDeductionRepository _allowanceDeductionRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ApplicationDbContext _context;

        public AllowanceDeductionController(
            IAllowanceDeductionRepository allowanceDeductionRepository,
            IEmployeeRepository employeeRepository,
            ApplicationDbContext context
        )
        {
            _allowanceDeductionRepository = allowanceDeductionRepository;
            _employeeRepository = employeeRepository;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllowanceDeductionsJson()
        {
            try
            {
                var allowanceDeductions =
                    await _allowanceDeductionRepository.GetAllAllowanceDeductionsAsync();
                var dtos = allowanceDeductions
                    .Select(a => new
                    {
                        a.AllowanceDeductionId,
                        a.AllowanceDeductionType,
                        a.AllowanceDeductionName,
                        a.CalculationType,
                        a.Percentage,
                        a.FixedAmount,
                        EffectiveFrom = a.EffectiveFrom.ToString("yyyy-MM-dd"),
                        EffectiveTo = a.EffectiveTo?.ToString("yyyy-MM-dd"),
                        a.IsActive,
                        a.IsCompanyWide,
                        EmployeeName = a.EmployeeId.HasValue
                            ? _context
                                .Employees.FirstOrDefault(e => e.EmployeeId == a.EmployeeId)
                                ?.FullName
                            : "All Employee",
                    })
                    .ToList();

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
            ViewBag.Title = "Create Allowance/Deduction";
            ViewBag.FormAction = Url.Action("Create");
            return View(
                "Create",
                new AllowanceDeductionDto
                {
                    IsActive = true,
                    EffectiveFrom = DateTime.Today,
                    IsCompanyWide = true,
                }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] AllowanceDeductionDto dto)
        {
            try
            {
                var entity = MapToEntity(dto);
                entity.AllowanceDeductionId = Guid.NewGuid();
                await _allowanceDeductionRepository.AddAllowanceDeductionAsync(entity);
                return Json(
                    new { success = true, message = "Allowance/Deduction created successfully." }
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
                    new
                    {
                        success = false,
                        message = $"Error creating allowance/deduction: {ex.Message}",
                    }
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var entity = await _allowanceDeductionRepository.GetAllowanceDeductionByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync(entity.EmployeeId);
            var dto = MapToDto(entity);
            ViewBag.Title = "Edit Allowance/Deduction";
            ViewBag.FormAction = Url.Action("Edit");
            return View("Create", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] AllowanceDeductionDto dto)
        {
            try
            {
                var entity = MapToEntity(dto);
                await _allowanceDeductionRepository.UpdateAllowanceDeductionAsync(entity);
                return Json(
                    new { success = true, message = "Allowance/Deduction updated successfully." }
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
                    new
                    {
                        success = false,
                        message = $"Error updating allowance/deduction: {ex.Message}",
                    }
                );
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var entity = await _allowanceDeductionRepository.GetAllowanceDeductionByIdAsync(id);
                if (entity == null)
                {
                    return NotFound(
                        new { success = false, message = "Allowance/Deduction not found." }
                    );
                }

                await _allowanceDeductionRepository.DeleteAllowanceDeductionAsync(id);
                return Json(
                    new { success = true, message = "Allowance/Deduction deleted successfully." }
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = $"Error deleting allowance/deduction: {ex.Message}",
                    }
                );
            }
        }

        private async Task PopulateDropdownsAsync(Guid? selectedEmployeeId = null)
        {
            var employees = await _employeeRepository.GetAllEmployeesAsync();
            ViewBag.Employees = new SelectList(
                employees.OrderBy(e => e.FullName),
                "EmployeeId",
                "FullName",
                selectedEmployeeId
            );
        }

        private static AllowanceDeduction MapToEntity(AllowanceDeductionDto dto)
        {
            return new AllowanceDeduction
            {
                AllowanceDeductionId = dto.AllowanceDeductionId,
                PayrollId = dto.PayrollId,
                EmployeeId = dto.EmployeeId,
                AllowanceDeductionType = dto.AllowanceDeductionType,
                AllowanceDeductionName = dto.AllowanceDeductionName,
                CalculationType = dto.CalculationType,
                Percentage = dto.Percentage,
                FixedAmount = dto.FixedAmount,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                IsActive = dto.IsActive,
                IsCompanyWide = dto.IsCompanyWide,
            };
        }

        private static AllowanceDeductionDto MapToDto(AllowanceDeduction entity)
        {
            return new AllowanceDeductionDto
            {
                AllowanceDeductionId = entity.AllowanceDeductionId,
                PayrollId = entity.PayrollId,
                EmployeeId = entity.EmployeeId,
                AllowanceDeductionType = entity.AllowanceDeductionType,
                AllowanceDeductionName = entity.AllowanceDeductionName,
                CalculationType = entity.CalculationType,
                Percentage = entity.Percentage,
                FixedAmount = entity.FixedAmount,
                EffectiveFrom = entity.EffectiveFrom,
                EffectiveTo = entity.EffectiveTo,
                IsActive = entity.IsActive,
                IsCompanyWide = entity.IsCompanyWide,
            };
        }
    }
}

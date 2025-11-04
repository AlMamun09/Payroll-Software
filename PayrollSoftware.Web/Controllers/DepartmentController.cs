using Microsoft.AspNetCore.Mvc;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Web.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly ApplicationDbContext _context;
        public DepartmentController(IDepartmentRepository departmentRepository, ApplicationDbContext context)
        {
            _departmentRepository = departmentRepository;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentsJson()
        {
            try
            {
                var departments = await _departmentRepository.GetAllDepartmentsAsync();
                var departmentDtos = departments.Select(d => new DepartmentDto
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentName = d.DepartmentName,
                    IsActive = d.IsActive
                }).ToList();
                return Json(new { data = departmentDtos });
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Create Department";
            ViewBag.FormAction = Url.Action("Create");
            return View("Create", new DepartmentDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] DepartmentDto departmentDto)
        {
            try
            {
                var entity = MapToEntity(departmentDto);
                entity.DepartmentId = Guid.NewGuid();
                await _departmentRepository.AddDepartmentAsync(entity);
                return Json(new { success = true, message = "Department created successfully." });
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
                return StatusCode(500, new { success = false, message = $"Error creating department: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var department = await _departmentRepository.GetDepartmentByIdAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            var departmentDto = MapToDto(department);
            ViewBag.Title = "Edit Department";
            ViewBag.FormAction = Url.Action("Edit");
            return View("Create", departmentDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] DepartmentDto departmentDto)
        {
            try
            {
                var entity = MapToEntity(departmentDto);
                await _departmentRepository.UpdateDepartmentAsync(entity);
                return Json(new { success = true, message = "Department updated successfully." });
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
                return StatusCode(500, new { success = false, message = $"Error updating department: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var department = await _departmentRepository.GetDepartmentByIdAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            await _departmentRepository.DeleteDepartmentAsync(id);
            return Json(new { success = true, message = "Department deleted successfully." });
        }

        private static Department MapToEntity(DepartmentDto departmentDto)
        {
            return new Department
            {
                DepartmentId = departmentDto.DepartmentId,
                DepartmentName = departmentDto.DepartmentName,
                IsActive = departmentDto.IsActive
            };
        }

        private static DepartmentDto MapToDto(Department department)
        {
            return new DepartmentDto
            {
                DepartmentId = department.DepartmentId,
                DepartmentName = department.DepartmentName,
                IsActive = department.IsActive,
            };
        }
    }
}

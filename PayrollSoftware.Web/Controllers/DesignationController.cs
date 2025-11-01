using Microsoft.AspNetCore.Mvc;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Web.Controllers
{
    public class DesignationController : Controller
    {
        private readonly IDesignationRepository _designationRepository;
        private readonly ApplicationDbContext _context;

        public DesignationController(IDesignationRepository designationRepository, ApplicationDbContext context)
        {
            _designationRepository = designationRepository;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDesignationsJson()
        {
            try
            {
                var designations = await _designationRepository.GetAllDesignationsAsync();
                var dtos = designations.Select(d => new DesignationDto
                {
                    DesignationId = d.DesignationId,
                    DesignationName = d.DesignationName,
                    IsActive = d.IsActive
                }).ToList();

                return Json(new { data = dtos });
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Create Designation";
            ViewBag.FormAction = Url.Action("Create");
            return View("Create", new DesignationDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] DesignationDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                return BadRequest(new { success = false, message = string.Join("\n", errors) });
            }

            var entity = MapToEntity(dto);
            entity.DesignationId = Guid.NewGuid();
            await _designationRepository.AddDesignationAsync(entity);
            return Json(new { success = true, message = "Designation created successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var entity = await _designationRepository.GetDesignationByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            var dto = MapToDto(entity);
            ViewBag.Title = "Edit Designation";
            ViewBag.FormAction = Url.Action("Edit");
            return View("Create", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] DesignationDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                return BadRequest(new { success = false, message = string.Join("\n", errors) });
            }

            var entity = MapToEntity(dto);
            await _designationRepository.UpdateDesignationAsync(entity);
            return Json(new { success = true, message = "Designation updated successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _designationRepository.GetDesignationByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            await _designationRepository.DeleteDesignationAsync(id);
            return Json(new { success = true, message = "Designation deleted successfully." });
        }

        private static Designation MapToEntity(DesignationDto dto)
        {
            return new Designation
            {
                DesignationId = dto.DesignationId,
                DesignationName = dto.DesignationName,
                IsActive = dto.IsActive
            };
        }

        private static DesignationDto MapToDto(Designation entity)
        {
            return new DesignationDto
            {
                DesignationId = entity.DesignationId,
                DesignationName = entity.DesignationName,
                IsActive = entity.IsActive
            };
        }
    }
}

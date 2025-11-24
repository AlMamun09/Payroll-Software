using Microsoft.AspNetCore.Mvc;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Web.Controllers
{
    public class LookupController : Controller
    {
        private readonly ILookupRepository _lookupRepository;

        public LookupController(ILookupRepository lookupRepository)
        {
            _lookupRepository = lookupRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetLookupsJson()
        {
            var lookups = await _lookupRepository.GetAllLookupsAsync();
            var dtos = lookups.Select(l => new
            {
                l.LookupId,
                l.LookupType,
                l.LookupValue,
                l.DisplayOrder,
                l.IsActive,
            });

            return Json(new { data = dtos });
        }

        [HttpGet]
        public async Task<IActionResult> GetLookupsByType(string lookupType)
        {
            var lookups = await _lookupRepository.GetLookupsByTypeAsync(lookupType);
            return Json(new { success = true, data = lookups });
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Title = "Create Lookup";
            ViewBag.FormAction = Url.Action("Create");
            ViewBag.LookupTypes = GetLookupTypes();
            return View(new LookupDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] LookupDto dto)
        {
            try
            {
                var entity = new Lookup
                {
                    LookupId = Guid.NewGuid(),
                    LookupType = dto.LookupType,
                    LookupValue = dto.LookupValue,
                    DisplayOrder = dto.DisplayOrder,
                    IsActive = dto.IsActive,
                };

                await _lookupRepository.AddLookupAsync(entity);
                return Json(new { success = true, message = "Lookup created successfully." });
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
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var entity = await _lookupRepository.GetLookupByIdAsync(id);
            if (entity == null)
                return NotFound();

            ViewBag.Title = "Edit Lookup";
            ViewBag.FormAction = Url.Action("Edit");
            ViewBag.LookupTypes = GetLookupTypes();

            var dto = new LookupDto
            {
                LookupId = entity.LookupId,
                LookupType = entity.LookupType,
                LookupValue = entity.LookupValue,
                DisplayOrder = entity.DisplayOrder,
                IsActive = entity.IsActive,
            };

            return View("Create", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] LookupDto dto)
        {
            try
            {
                var entity = new Lookup
                {
                    LookupId = dto.LookupId,
                    LookupType = dto.LookupType,
                    LookupValue = dto.LookupValue,
                    DisplayOrder = dto.DisplayOrder,
                    IsActive = dto.IsActive,
                };

                await _lookupRepository.UpdateLookupAsync(entity);
                return Json(new { success = true, message = "Lookup updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            try
            {
                await _lookupRepository.DeleteLookupAsync(id);
                return Json(new { success = true, message = "Lookup deactivated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(Guid id)
        {
            try
            {
                var lookup = await _lookupRepository.GetLookupByIdAsync(id);
                if (lookup == null)
                    return NotFound(new { success = false, message = "Lookup not found." });

                lookup.IsActive = true;
                await _lookupRepository.UpdateLookupAsync(lookup);
                return Json(new { success = true, message = "Lookup activated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _lookupRepository.DeleteLookupAsync(id);
                return Json(new { success = true, message = "Lookup deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private List<string> GetLookupTypes()
        {
            return new List<string>
            {
                "Gender",
                "LeaveType",
                "EmploymentType",
                "PaymentSystem",
                "Status",
                "LeaveStatus",
                "AttendanceStatus",
                "Department",
                "Designation",
                "CalculationType",
                "AllowanceDeductionType",
                "Weekend",
            };
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Web.Controllers
{
    public class ShiftController : Controller
    {
        private readonly IShiftRepository _shiftRepository;
        private readonly ApplicationDbContext _context;

        public ShiftController(IShiftRepository shiftRepository, ApplicationDbContext context)
        {
            _shiftRepository = shiftRepository;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetShiftsJson()
        {
            try
            {
                var shifts = await _shiftRepository.GetAllShiftsAsync();
                var dtos = shifts.Select(s => new ShiftDto
                {
                    ShiftId = s.ShiftId,
                    ShiftName = s.ShiftName,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsActive = s.IsActive
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
            ViewBag.Title = "Create Shift";
            ViewBag.FormAction = Url.Action("Create");
            return View("Create", new ShiftDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] ShiftDto dto)
        {
            try
            {
                var entity = MapToEntity(dto);
                entity.ShiftId = Guid.NewGuid();
                await _shiftRepository.AddShiftAsync(entity);
                return Json(new { success = true, message = "Shift created successfully." });
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
                return StatusCode(500, new { success = false, message = $"Error creating shift: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var shift = await _shiftRepository.GetShiftByIdAsync(id);
            if (shift == null)
            {
                return NotFound();
            }

            var dto = MapToDto(shift);
            ViewBag.Title = "Edit Shift";
            ViewBag.FormAction = Url.Action("Edit");
            return View("Create", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] ShiftDto dto)
        {
            try
            {
                var entity = MapToEntity(dto);
                await _shiftRepository.UpdateShiftAsync(entity);
                return Json(new { success = true, message = "Shift updated successfully." });
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
                return StatusCode(500, new { success = false, message = $"Error updating shift: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var shift = await _shiftRepository.GetShiftByIdAsync(id);
            if (shift == null)
            {
                return NotFound();
            }
            await _shiftRepository.DeleteShiftAsync(id);
            return Json(new { success = true, message = "Shift deleted successfully." });
        }

        private static Shift MapToEntity(ShiftDto dto)
        {
            return new Shift
            {
                ShiftId = dto.ShiftId,
                ShiftName = dto.ShiftName,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsActive = dto.IsActive
            };
        }

        private static ShiftDto MapToDto(Shift entity)
        {
            return new ShiftDto
            {
                ShiftId = entity.ShiftId,
                ShiftName = entity.ShiftName,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                IsActive = entity.IsActive
            };
        }
    }
}

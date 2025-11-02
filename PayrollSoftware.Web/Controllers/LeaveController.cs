using Microsoft.AspNetCore.Mvc;
using PayrollSoftware.Infrastructure.Application.DTOs;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Controllers
{
    public class LeaveController : Controller
    {
        private readonly ILeaveRepository _leaveRepository;

        public LeaveController(ILeaveRepository leaveRepository)
        {
            _leaveRepository = leaveRepository;
        }

        // GET: /Leave
        public async Task<IActionResult> Index()
        {
            try
            {
                var leaves = await _leaveRepository.GetAllLeavesAsync();
                var leaveDtos = MapToDtoList(leaves);
                return View(leaveDtos);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving leaves: {ex.Message}";
                return View(new List<LeaveDto>());
            }
        }

        // GET: /Leave/GetLeavesJson
        [HttpGet]
        public async Task<IActionResult> GetLeavesJson()
        {
            try
            {
                var leaves = await _leaveRepository.GetAllLeavesAsync();
                var leaveDtos = MapToDtoList(leaves);

                return Json(new
                {
                    data = leaveDtos
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = $"Error retrieving leaves: {ex.Message}"
                });
            }
        }

        // GET: /Leave/EmployeeLeaves/{employeeId}
        public async Task<IActionResult> EmployeeLeaves(Guid employeeId)
        {
            try
            {
                var leaves = await _leaveRepository.GetLeavesByEmployeeIdAsync(employeeId);
                var leaveDtos = MapToDtoList(leaves);
                return View(leaveDtos);
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving employee leaves: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Leave/Pending
        public async Task<IActionResult> Pending()
        {
            try
            {
                var leaves = await _leaveRepository.GetPendingLeavesAsync();
                var leaveDtos = MapToDtoList(leaves);
                return View(leaveDtos);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving pending leaves: {ex.Message}";
                return View(new List<LeaveDto>());
            }
        }

        // GET: /Leave/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var leave = await _leaveRepository.GetLeaveByIdAsync(id);
                if (leave == null)
                {
                    TempData["ErrorMessage"] = $"Leave with ID {id} not found.";
                    return RedirectToAction(nameof(Index));
                }

                var leaveDto = MapToDto(leave);
                return View(leaveDto);
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving leave details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Leave/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Leave/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveDto leaveDto)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var leave = MapToEntity(leaveDto);
                    var createdLeave = await _leaveRepository.ApplyForLeaveAsync(leave);
                    TempData["SuccessMessage"] = "Leave application submitted successfully!";
                    return RedirectToAction(nameof(Details), new { id = createdLeave.LeaveId });
                }
                return View(leaveDto);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(leaveDto);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(leaveDto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error applying for leave: {ex.Message}";
                return View(leaveDto);
            }
        }

        // GET: /Leave/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var leave = await _leaveRepository.GetLeaveByIdAsync(id);
                if (leave == null)
                {
                    TempData["ErrorMessage"] = $"Leave with ID {id} not found.";
                    return RedirectToAction(nameof(Index));
                }

                var leaveDto = MapToDto(leave);
                return View(leaveDto);
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving leave for editing: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Leave/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, LeaveDto leaveDto)
        {
            try
            {
                if (id != leaveDto.LeaveId)
                {
                    TempData["ErrorMessage"] = "Leave ID mismatch.";
                    return View(leaveDto);
                }

                if (ModelState.IsValid)
                {
                    var leave = MapToEntity(leaveDto);
                    await _leaveRepository.UpdateLeaveStatusAsync(leave.LeaveId, leave.LeaveStatus, leave.Remarks);
                    TempData["SuccessMessage"] = "Leave updated successfully!";
                    return RedirectToAction(nameof(Details), new { id = leave.LeaveId });
                }
                return View(leaveDto);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(leaveDto);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(leaveDto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating leave: {ex.Message}";
                return View(leaveDto);
            }
        }

        // GET: /Leave/Approve/{id}
        public async Task<IActionResult> Approve(Guid id)
        {
            try
            {
                var leave = await _leaveRepository.GetLeaveByIdAsync(id);
                if (leave == null)
                {
                    TempData["ErrorMessage"] = $"Leave with ID {id} not found.";
                    return RedirectToAction(nameof(Index));
                }

                var leaveDto = MapToDto(leave);
                return View(leaveDto);
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving leave for approval: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Leave/Approve/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, string remarks)
        {
            try
            {
                var updatedLeave = await _leaveRepository.UpdateLeaveStatusAsync(id, "Approved", remarks);
                TempData["SuccessMessage"] = "Leave approved successfully!";
                return RedirectToAction(nameof(Details), new { id = updatedLeave.LeaveId });
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Approve), new { id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Approve), new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error approving leave: {ex.Message}";
                return RedirectToAction(nameof(Approve), new { id });
            }
        }

        // GET: /Leave/Reject/{id}
        public async Task<IActionResult> Reject(Guid id)
        {
            try
            {
                var leave = await _leaveRepository.GetLeaveByIdAsync(id);
                if (leave == null)
                {
                    TempData["ErrorMessage"] = $"Leave with ID {id} not found.";
                    return RedirectToAction(nameof(Index));
                }

                var leaveDto = MapToDto(leave);
                return View(leaveDto);
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving leave for rejection: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Leave/Reject/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string remarks)
        {
            try
            {
                var updatedLeave = await _leaveRepository.UpdateLeaveStatusAsync(id, "Rejected", remarks);
                TempData["SuccessMessage"] = "Leave rejected successfully!";
                return RedirectToAction(nameof(Details), new { id = updatedLeave.LeaveId });
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Reject), new { id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Reject), new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error rejecting leave: {ex.Message}";
                return RedirectToAction(nameof(Reject), new { id });
            }
        }

        // POST: /Leave/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _leaveRepository.DeleteLeaveAsync(id);
                TempData["SuccessMessage"] = "Leave deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting leave: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: /Leave/CheckOverlap
        public async Task<IActionResult> CheckOverlap(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeLeaveId = null)
        {
            try
            {
                var hasOverlap = await _leaveRepository.HasLeaveOverlapAsync(employeeId, startDate, endDate, excludeLeaveId);
                return Json(new { hasOverlap });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        #region Helper Methods

        private LeaveDto MapToDto(Leave leave)
        {
            return new LeaveDto
            {
                LeaveId = leave.LeaveId,
                EmployeeId = leave.EmployeeId,
                LeaveType = leave.LeaveType,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                TotalDays = leave.TotalDays,
                LeaveStatus = leave.LeaveStatus,
                Remarks = leave.Remarks
            };
        }

        private List<LeaveDto> MapToDtoList(List<Leave> leaves)
        {
            var leaveDtos = new List<LeaveDto>();
            foreach (var leave in leaves)
            {
                leaveDtos.Add(MapToDto(leave));
            }
            return leaveDtos;
        }

        private Leave MapToEntity(LeaveDto leaveDto)
        {
            return new Leave
            {
                LeaveId = leaveDto.LeaveId,
                EmployeeId = leaveDto.EmployeeId,
                LeaveType = leaveDto.LeaveType,
                StartDate = leaveDto.StartDate,
                EndDate = leaveDto.EndDate,
                TotalDays = leaveDto.TotalDays,
                LeaveStatus = leaveDto.LeaveStatus,
                Remarks = leaveDto.Remarks
            };
        }

        #endregion
    }
}
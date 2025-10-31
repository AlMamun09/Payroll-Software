using Microsoft.AspNetCore.Mvc;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Domain.Entities;

namespace PayrollSoftware.Web.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(IEmployeeRepository employeeRepository, ILogger<EmployeeController> logger)
        {
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        // GET: /Employee
        public async Task<IActionResult> Index()
        {
            try
            {
                var employees = await _employeeRepository.GetAllAsync();
                return View(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees for index page");
                TempData["Error"] = "Error loading employees. Please try again.";
                return View(new List<Employee>());
            }
        }

        // GET: /Employee/Create
        public IActionResult Create()
        {
            var employee = new Employee
            {
                Status = "Currently Active" // Default value
            };
            return View(employee);
        }

        // POST: /Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var createdEmployee = await _employeeRepository.CreateAsync(employee);
                    TempData["Success"] = $"Employee {createdEmployee.EmployeeCode} created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                // If we got this far, something failed, redisplay form
                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                ModelState.AddModelError("", ex.Message);
                return View(employee);
            }
        }

        // GET: /Employee/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var employee = await _employeeRepository.GetByIdAsync(id);
                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View("Create", employee); // Reuse the Create view for editing
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee for edit with ID {EmployeeId}", id);
                TempData["Error"] = "Error loading employee for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Employee/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Employee employee)
        {
            try
            {
                if (id != employee.EmployeeId)
                {
                    TempData["Error"] = "Employee ID mismatch.";
                    return RedirectToAction(nameof(Index));
                }

                if (ModelState.IsValid)
                {
                    var updatedEmployee = await _employeeRepository.UpdateAsync(employee);
                    TempData["Success"] = $"Employee {updatedEmployee.EmployeeCode} updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                return View("Create", employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee with ID {EmployeeId}", id);
                ModelState.AddModelError("", ex.Message);
                return View("Create", employee);
            }
        }

        // POST: /Employee/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _employeeRepository.DeleteAsync(id);
                if (result)
                {
                    TempData["Success"] = "Employee deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Employee not found.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee with ID {EmployeeId}", id);
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Employee/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var employee = await _employeeRepository.GetByIdAsync(id);
                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee details with ID {EmployeeId}", id);
                TempData["Error"] = "Error loading employee details.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DriverTracker.Data;
using DriverTracker.Models;
using Microsoft.AspNetCore.Authorization;
using DriverTracker.Services;
using Microsoft.AspNetCore.Identity;


namespace DriverTracker.Controllers
{
    [Authorize(Roles = "Admin, Employee")]
    public class EmployeesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmployeesController(
            AppDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
            {
                var employees = await _context.Employees.ToListAsync();
                return View(employees);
            }
            else
            {
                var employeeEmail = User.Identity.Name;
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == employeeEmail);

                if (employee != null)
                {
                    return View(new List<Employee> { employee });
                }
            }
            return Forbid();
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.EmployeeID == id);

            if (employee == null) return NotFound();

            // Kontrollera att employee bara kan se sina egna detaljer
            if (!User.IsInRole("Admin") && User.Identity.Name != employee.Email)
            {
                return Forbid();
            }

            return View(employee);
        }

        // GET: Employees/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Name,Email,Password")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                // Kontrollera om Employee-rollen finns
                if (!await _roleManager.RoleExistsAsync("Employee"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Employee"));
                }

                // Skapa Identity user
                var user = new IdentityUser { UserName = employee.Email, Email = employee.Email };
                var result = await _userManager.CreateAsync(user, employee.Password);

                if (result.Succeeded)
                {
                    // Lägg till i Employee-rollen
                    await _userManager.AddToRoleAsync(user, "Employee");

                    // Spara employee i vår custom tabell
                    employee.Role = "Employee";
                    _context.Add(employee);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            // Kontrollera behörighet
            if (!User.IsInRole("Admin") && User.Identity.Name != employee.Email)
            {
                return Forbid();
            }

            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeID,Name,Email,Password,Role")] Employee employee)
        {
            if (id != employee.EmployeeID) return NotFound();

            // Kontrollera behörighet
            if (!User.IsInRole("Admin") && User.Identity.Name != employee.Email)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var identityUser = await _userManager.FindByEmailAsync(employee.Email);
                    if (identityUser != null)
                    {
                        // Uppdatera Identity user
                        identityUser.Email = employee.Email;
                        identityUser.UserName = employee.Email;
                        await _userManager.UpdateAsync(identityUser);

                        // Uppdatera lösenord om ett nytt angivits
                        if (!string.IsNullOrEmpty(employee.Password))
                        {
                            var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                            await _userManager.ResetPasswordAsync(identityUser, token, employee.Password);
                        }
                    }

                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmployeeID))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(employee);
        }

        // GET: Employees/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.EmployeeID == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            // Ta bort från Identity system
            var identityUser = await _userManager.FindByEmailAsync(employee.Email);
            if (identityUser != null)
            {
                await _userManager.DeleteAsync(identityUser);
            }

            // Ta bort från vår custom tabell
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Behåll dina befintliga ChangePassword-metoder här...
        // GET: Employees/ChangePassword
        public async Task<IActionResult> ChangePassword(int? id)
        {
            // Om admin ändrar lösenord för en annan employee
            if (User.IsInRole("Admin") && id.HasValue)
            {
                var employeeToChange = await _context.Employees.FindAsync(id);
                if (employeeToChange == null)
                {
                    return NotFound();
                }

                var modell = new ChangePasswordViewModel { Email = employeeToChange.Email };
                return View(modell);
            }

            // Om employee ändrar sitt eget lösenord
            var employeeEmail = User.Identity.Name;
            var model = new ChangePasswordViewModel { Email = employeeEmail };
            return View(model);
        }

        // POST: Employees/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound();
            }

            // Om det är admin som ändrar lösenord för en annan employee
            if (User.IsInRole("Admin") && User.Identity.Name != model.Email)
            {
                var result = await _userManager.ResetPasswordAsync(user,
                    await _userManager.GeneratePasswordResetTokenAsync(user),
                    model.NewPassword);

                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            // Om employee ändrar sitt eget lösenord
            else
            {
                var result = await _userManager.ChangePasswordAsync(user,
                    model.CurrentPassword,
                    model.NewPassword);

                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
            }

            //foreach (var error in result.Errors)
            //{
            //    ModelState.AddModelError("", error.Description);
            //}

            return View(model);
        }
        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeID == id);
        }
    }
}

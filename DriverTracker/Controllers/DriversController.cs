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
using Microsoft.AspNetCore.Identity;

namespace DriverTracker.Controllers
{
    [Authorize(Roles = "Admin, Employee")]
    public class DriversController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DriversController
            (AppDbContext context, 
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Drivers
        public async Task<IActionResult> Index(string searchString)
        {
            IQueryable<Driver> query = _context.Drivers
            .Include(d => d.ResponsibleEmployee);

            if (User.IsInRole("Employee"))
            {
                // Hämta inloggad användares email
                var user = await _userManager.GetUserAsync(User);
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null)
                {
                    return NotFound();
                }

                // Filtrera på ansvarig employee
                query = query.Where(d => d.ResponsibleEmployeeId == employee.EmployeeID);
            }

            // Applicera sökning om searchString finns
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(d => d.DriverName.Contains(searchString) ||
                                        d.CarReg.Contains(searchString));
            }

            var drivers = await query.OrderBy(d => d.DriverName).ToListAsync();
            return View(drivers);
        }

        // GET: Drivers/Details/5
        public async Task<IActionResult> Details(int? id, DateTime? fromDate, DateTime? toDate)
        {
            if (id == null) return NotFound();

            var driver = await _context.Drivers
                .Include(d => d.ResponsibleEmployee)
                .Include(d => d.Events)
                .FirstOrDefaultAsync(d => d.DriverID == id);

            if (driver == null) return NotFound();

            // Kontrollera behörighet för Employee
            if (User.IsInRole("Employee"))
            {
                var user = await _userManager.GetUserAsync(User);
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null || driver.ResponsibleEmployeeId != employee.EmployeeID)
                    return Forbid();
            }

            // Filtrera events om datum angivits
            var events = driver.Events.AsQueryable();
            if (fromDate.HasValue)
            {
                events = events.Where(e => e.EventDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                events = events.Where(e => e.EventDate <= toDate.Value);
            }

            driver.Events = events.OrderByDescending(e => e.EventDate).ToList();

            return View(driver);
        }

        // GET: Drivers/Create
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Admin"))
            {
                // Admin kan välja bland alla employees
                ViewData["ResponsibleEmployeeId"] = new SelectList(
                    await _context.Employees.ToListAsync(),
                    "EmployeeID",
                    "Name"
                );
            }
            else
            {
                // För employee, sätt automatiskt den inloggade användaren
                var user = await _userManager.GetUserAsync(User);
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null)
                {
                    return NotFound();
                }

                ViewData["ResponsibleEmployeeId"] = new SelectList(
                    new[] { employee },
                    "EmployeeID",
                    "Name"
                );
            }

            return View();
        }

        // POST: Drivers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DriverName,CarReg,ResponsibleEmployeeId")] Driver driver)
        {
            if (ModelState.IsValid)
            {
                if (!User.IsInRole("Admin"))
                {
                    // För employee, sätt automatiskt den inloggade användaren
                    var user = await _userManager.GetUserAsync(User);
                    var employee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Email == user.Email);

                    if (employee == null)
                    {
                        return NotFound();
                    }

                    driver.ResponsibleEmployeeId = employee.EmployeeID;
                }

                _context.Add(driver);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Om något går fel, återskapa dropdown
            if (User.IsInRole("Admin"))
            {
                ViewData["ResponsibleEmployeeId"] = new SelectList(
                    await _context.Employees.ToListAsync(),
                    "EmployeeID",
                    "Name",
                    driver.ResponsibleEmployeeId
                );
            }
            return View(driver);
        }

        // GET: Drivers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null)
            {
                return NotFound();
            }
            ViewData["ResponsibleEmployeeId"] = new SelectList(_context.Employees, "EmployeeID", "Email", driver.ResponsibleEmployeeId);
            return View(driver);
        }

        // POST: Drivers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DriverID,DriverName,CarReg,ResponsibleEmployeeId")] Driver driver)
        {
            if (id != driver.DriverID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(driver);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DriverExists(driver.DriverID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ResponsibleEmployeeId"] = new SelectList(_context.Employees, "EmployeeID", "Email", driver.ResponsibleEmployeeId);
            return View(driver);
        }

        // GET: Drivers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers
                .Include(d => d.ResponsibleEmployee)
                .FirstOrDefaultAsync(m => m.DriverID == id);
            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        // POST: Drivers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver != null)
            {
                _context.Drivers.Remove(driver);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DriverExists(int id)
        {
            return _context.Drivers.Any(e => e.DriverID == id);
        }
    }
}

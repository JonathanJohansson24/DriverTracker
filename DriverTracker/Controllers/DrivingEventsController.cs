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
    public class DrivingEventsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DrivingEventsController(AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: DrivingEvents
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            IQueryable<DrivingEvents> query = _context.Events
                .Include(d => d.Driver)
                .Include(d => d.Driver.ResponsibleEmployee);

            if (User.IsInRole("Admin"))
            {
                // Admin ser alla events
                query = query;
            }
            else
            {
                // Hämta inloggad användare
                var user = await _userManager.GetUserAsync(User);
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null)
                {
                    return NotFound();
                }

                // Filtrera på ansvarig employee
                query = query.Where(e => e.Driver.ResponsibleEmployeeId == employee.EmployeeID);
            }

            // Datumfiltrering
            if (fromDate.HasValue)
            {
                query = query.Where(e => e.EventDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                query = query.Where(e => e.EventDate <= toDate.Value);
            }

            // Sortera på datum, senaste först
            var events = await query.OrderByDescending(e => e.EventDate).ToListAsync();
            return View(events);
        }

        // GET: DrivingEvents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var drivingEvent = await _context.Events
                .Include(d => d.Driver)
                .Include(d => d.Driver.ResponsibleEmployee)
                .FirstOrDefaultAsync(m => m.DriverEventId == id);

            if (drivingEvent == null)
            {
                return NotFound();
            }

            // Kontrollera om användaren har rätt att se denna händelse
            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null || drivingEvent.Driver.ResponsibleEmployeeId != employee.EmployeeID)
                {
                    return Unauthorized();
                }
            }

            return View(drivingEvent);
        }

        // GET: DrivingEvents/Create
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Admin"))
            {
                // Admin kan välja bland alla förare
                ViewData["DriverId"] = new SelectList(_context.Drivers, "DriverID", "DriverName");
            }
            else
            {
                // Employee kan bara välja bland sina förare
                var user = await _userManager.GetUserAsync(User);
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null)
                {
                    return NotFound();
                }

                ViewData["DriverId"] = new SelectList(
                    _context.Drivers.Where(d => d.ResponsibleEmployeeId == employee.EmployeeID),
                    "DriverID",
                    "DriverName"
                );
            }
            return View();
        }

        // POST: DrivingEvents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Description,EventDate,AmountOut,AmountIn,DriverId")] DrivingEvents drivingEvents)
        {
            if (ModelState.IsValid)
            {
                if (!User.IsInRole("Admin"))
                {
                    // Verify that the employee is responsible for this driver
                    var user = await _userManager.GetUserAsync(User);
                    var employee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Email == user.Email);
                    var driver = await _context.Drivers
                        .FirstOrDefaultAsync(d => d.DriverID == drivingEvents.DriverId);

                    if (employee == null || driver == null || driver.ResponsibleEmployeeId != employee.EmployeeID)
                    {
                        return Unauthorized();
                    }
                }

                _context.Add(drivingEvents);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Om vi kommer hit har något gått fel, återskapa SelectList
            await SetupDriverSelectList(drivingEvents.DriverId);
            return View(drivingEvents);
        }

        // GET: DrivingEvents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var drivingEvent = await _context.Events
                .Include(d => d.Driver)
                .FirstOrDefaultAsync(d => d.DriverEventId == id);

            if (drivingEvent == null)
            {
                return NotFound();
            }

            // Kontrollera behörighet
            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null || drivingEvent.Driver.ResponsibleEmployeeId != employee.EmployeeID)
                {
                    return Unauthorized();
                }
            }

            await SetupDriverSelectList(drivingEvent.DriverId);
            return View(drivingEvent);
        }

        // POST: DrivingEvents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DriverEventId,Description,EventDate,AmountOut,AmountIn,DriverId")] DrivingEvents drivingEvents)
        {
            if (id != drivingEvents.DriverEventId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kontrollera behörighet
                    if (!User.IsInRole("Admin"))
                    {
                        var user = await _userManager.GetUserAsync(User);
                        var employee = await _context.Employees
                            .FirstOrDefaultAsync(e => e.Email == user.Email);
                        var driver = await _context.Drivers
                            .FirstOrDefaultAsync(d => d.DriverID == drivingEvents.DriverId);

                        if (employee == null || driver == null || driver.ResponsibleEmployeeId != employee.EmployeeID)
                        {
                            return Unauthorized();
                        }
                    }

                    _context.Update(drivingEvents);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DrivingEventsExists(drivingEvents.DriverEventId))
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

            await SetupDriverSelectList(drivingEvents.DriverId);
            return View(drivingEvents);
        }

        // GET: DrivingEvents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var drivingEvent = await _context.Events
                .Include(d => d.Driver)
                .Include(d => d.Driver.ResponsibleEmployee)
                .FirstOrDefaultAsync(m => m.DriverEventId == id);

            if (drivingEvent == null)
            {
                return NotFound();
            }

            // Kontrollera behörighet
            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null || drivingEvent.Driver.ResponsibleEmployeeId != employee.EmployeeID)
                {
                    return Unauthorized();
                }
            }

            return View(drivingEvent);
        }

        // POST: DrivingEvents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var drivingEvent = await _context.Events
                .Include(d => d.Driver)
                .FirstOrDefaultAsync(d => d.DriverEventId == id);

            if (drivingEvent == null)
            {
                return NotFound();
            }

            // Kontrollera behörighet
            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null || drivingEvent.Driver.ResponsibleEmployeeId != employee.EmployeeID)
                {
                    return Unauthorized();
                }
            }

            _context.Events.Remove(drivingEvent);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DrivingEventsExists(int id)
        {
            return _context.Events.Any(e => e.DriverEventId == id);
        }

        // Hjälpmetod för att sätta upp DriverSelectList
        private async Task SetupDriverSelectList(int? selectedDriverId = null)
        {
            if (User.IsInRole("Admin"))
            {
                ViewData["DriverId"] = new SelectList(_context.Drivers, "DriverID", "DriverName", selectedDriverId);
            }
            else
            {
                var user = await _userManager.GetUserAsync(User);
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                ViewData["DriverId"] = new SelectList(
                    _context.Drivers.Where(d => d.ResponsibleEmployeeId == employee.EmployeeID),
                    "DriverID",
                    "DriverName",
                    selectedDriverId
                );
            }
        }
        public async Task<IActionResult> Notifications()
        {
            IQueryable<DrivingEvents> query = _context.Events
                .Include(d => d.Driver)
                .Include(d => d.Driver.ResponsibleEmployee)
                .Where(e => e.EventDate >= DateTime.Now.AddHours(-12));

            if (User.IsInRole("Employee"))
            {
                var user = await _userManager.GetUserAsync(User);
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null)
                {
                    return NotFound();
                }

                // Filtrera på ansvarig employee
                query = query.Where(e => e.Driver.ResponsibleEmployeeId == employee.EmployeeID);
            }

            var events = await query.OrderByDescending(e => e.EventDate).ToListAsync();
            return View(events);
        }
    }
}

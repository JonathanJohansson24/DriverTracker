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
using DriverTracker.Services;

namespace DriverTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAdminService _adminService;

        public AdminsController(AppDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IAdminService adminService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _adminService = adminService;
        }

        // GET: Admins
        public IActionResult Index()
        {
            return View();
        }

        // GET: Admins/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var admin = await _context.Admins
                .FirstOrDefaultAsync(m => m.AdminID == id);
            if (admin == null)
            {
                return NotFound();
            }

            return View(admin);
        }

        // GET: Admins/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admins/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Password")] Admin admin)
        {
            if (ModelState.IsValid)
            {
                // Kontrollera om Admin-rollen finns, annars skapa den
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                // Skapa en ny IdentityUser baserat på Admin-modellen
                var adminUser = new IdentityUser { UserName = admin.Email, Email = admin.Email };

                // Skapa användaren med lösenord
                var result = await _userManager.CreateAsync(adminUser, admin.Password);
                if (result.Succeeded)
                {
                    // Lägg till användaren i Admin-rollen
                    await _userManager.AddToRoleAsync(adminUser, "Admin");

                    // Spara admin-information i databasen om du fortfarande vill lagra extra admin-information
                    admin.Role = "Admin";
                    _context.Admins.Add(admin);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }

                // Hantera eventuella fel från UserManager
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(admin);
        }


        // GET: Admins/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var admin = await _context.Admins.FindAsync(id);
            if (admin == null)
            {
                return NotFound();
            }

            // Hämta IdentityUser baserat på admin-e-post
            var identityAdmin = await _userManager.FindByEmailAsync(admin.Email);
            if (identityAdmin == null)
            {
                return NotFound();
            }

            // Vi kan även skicka med nuvarande e-post och roll till vyn om vi vill
            ViewBag.CurrentEmail = identityAdmin.Email;
            ViewBag.CurrentRole = "Admin";

            return View(admin);
        }

        // POST: Admins/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AdminID,Name,Email,Password")] Admin admin)
        {
            if (id != admin.AdminID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var identityAdmin = await _userManager.FindByEmailAsync(admin.Email);
                    if (identityAdmin == null)
                    {
                        return NotFound();
                    }

                    // Uppdatera e-post om det har ändrats
                    if (identityAdmin.Email != admin.Email)
                    {
                        identityAdmin.Email = admin.Email;
                        identityAdmin.UserName = admin.Email;
                        await _userManager.UpdateAsync(identityAdmin);
                    }

                    // Uppdatera lösenord om ett nytt lösenord tillhandahålls
                    if (!string.IsNullOrEmpty(admin.Password))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(identityAdmin);
                        var result = await _userManager.ResetPasswordAsync(identityAdmin, token, admin.Password);
                        if (!result.Succeeded)
                        {
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError("", error.Description);
                            }
                            return View(admin);
                        }
                    }

                    // Uppdatera eventuell extra admin-information
                    _context.Update(admin);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdminExists(admin.AdminID))
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
            return View(admin);
        }

        // GET: Admins/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var admin = await _context.Admins
                .FirstOrDefaultAsync(m => m.AdminID == id);
            if (admin == null)
            {
                return NotFound();
            }

            return View(admin);
        }

        // POST: Admins/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin != null)
            {
                var identityAdmin = await _userManager.FindByEmailAsync(admin.Email);
                if (identityAdmin != null)
                {
                    // Ta bort från Identity-systemet
                    await _userManager.DeleteAsync(identityAdmin);
                }

                // Ta bort från databasen
                _context.Admins.Remove(admin);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AdminExists(int id)
        {
            return _context.Admins.Any(e => e.AdminID == id);
        }
    }
}

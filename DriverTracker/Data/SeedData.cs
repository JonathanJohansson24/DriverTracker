using DriverTracker.Models;
using Microsoft.AspNetCore.Identity;

namespace DriverTracker.Data
{
    public class SeedData
    {
        public static async Task Initialize(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
        {
            Console.WriteLine("Starting SeedData Initialization...");

            // Ensure roles are created
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!await roleManager.RoleExistsAsync("Employee"))
            {
                await roleManager.CreateAsync(new IdentityRole("Employee"));
            }

            // Create admin user
            var adminEmail = "alice.admin@example.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
                await userManager.CreateAsync(adminUser, "AdminPassword123!");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // Create employee users with real names and seed Employee table
            var employees = new List<(string name, string email)>
            {
                ("John Smith", "john.smith@example.com"),
                ("Emily Johnson", "emily.johnson@example.com"),
                ("Michael Brown", "michael.brown@example.com"),
                ("Sarah Williams", "sarah.williams@example.com")
            };

            foreach (var (name, email) in employees)
            {
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var employeeUser = new IdentityUser { UserName = email, Email = email };
                    await userManager.CreateAsync(employeeUser, "EmployeePassword123!");
                    await userManager.AddToRoleAsync(employeeUser, "Employee");
                }

                // Seed Employees to the Employee table
                if (!context.Employees.Any(e => e.Email == email))
                {
                    context.Employees.Add(new Employee
                    {
                        Name = name,
                        Email = email,
                        Password = "EmployeePassword123!",
                        Role = "Employee"
                    });
                }
            }

            // Save the employees to the database
            await context.SaveChangesAsync(); // Save employees before drivers

            // Seed Drivers with realistic names AFTER employees are added
            var drivers = new List<Driver>
            {
                new Driver { DriverName = "David Turner", CarReg = "ABC123", ResponsibleEmployeeId = context.Employees.First(e => e.Email == "john.smith@example.com").EmployeeID },
                new Driver { DriverName = "Sophia Carter", CarReg = "XYZ456", ResponsibleEmployeeId = context.Employees.First(e => e.Email == "emily.johnson@example.com").EmployeeID },
                new Driver { DriverName = "James Adams", CarReg = "DEF789", ResponsibleEmployeeId = context.Employees.First(e => e.Email == "michael.brown@example.com").EmployeeID },
                new Driver { DriverName = "Olivia Evans", CarReg = "GHI012", ResponsibleEmployeeId = context.Employees.First(e => e.Email == "john.smith@example.com").EmployeeID },
                new Driver { DriverName = "Daniel Miller", CarReg = "JKL345", ResponsibleEmployeeId = context.Employees.First(e => e.Email == "emily.johnson@example.com").EmployeeID },
                new Driver { DriverName = "Amelia Thomas", CarReg = "MNO678", ResponsibleEmployeeId = context.Employees.First(e => e.Email == "michael.brown@example.com").EmployeeID },
                new Driver { DriverName = "Christopher Harris", CarReg = "PQR901", ResponsibleEmployeeId = context.Employees.First(e => e.Email == "sarah.williams@example.com").EmployeeID },
                new Driver { DriverName = "Isabella Clark", CarReg = "STU234", ResponsibleEmployeeId = context.Employees.First(e => e.Email == "john.smith@example.com").EmployeeID },
                new Driver { DriverName = "Matthew Lewis", CarReg = "VWX567", ResponsibleEmployeeId = context.Employees.First(e => e.Email == "emily.johnson@example.com").EmployeeID },
                new Driver { DriverName = "Chloe King", CarReg = "YZA890", ResponsibleEmployeeId = context.Employees.First(e => e.Email == "sarah.williams@example.com").EmployeeID }
            };

            if (!context.Drivers.Any())
            {
                context.Drivers.AddRange(drivers);
                await context.SaveChangesAsync(); // Save drivers before adding events
            }

            // Fetch the Driver IDs after saving
            var driverIds = context.Drivers.ToList();

            // Seed Driving Events after drivers are saved
            if (!context.Events.Any())
            {
                context.Events.AddRange(
                    // Existing events
                    new DrivingEvents { DriverId = driverIds[0].DriverID, Description = "Fueling", EventDate = DateTime.Now.AddDays(-1), AmountOut = 50, AmountIn = 0 },
                    new DrivingEvents { DriverId = driverIds[0].DriverID, Description = "Car Wash", EventDate = DateTime.Now.AddDays(-2), AmountOut = 30, AmountIn = 0 },
                    new DrivingEvents { DriverId = driverIds[1].DriverID, Description = "Maintenance", EventDate = DateTime.Now.AddDays(-3), AmountOut = 200, AmountIn = 0 },
                    new DrivingEvents { DriverId = driverIds[1].DriverID, Description = "Fueling", EventDate = DateTime.Now.AddDays(-4), AmountOut = 60, AmountIn = 0 },
                    new DrivingEvents { DriverId = driverIds[2].DriverID, Description = "Car Wash", EventDate = DateTime.Now.AddDays(-5), AmountOut = 30, AmountIn = 0 },
                    new DrivingEvents { DriverId = driverIds[2].DriverID, Description = "Tire Replacement", EventDate = DateTime.Now.AddDays(-6), AmountOut = 150, AmountIn = 0 },
                    new DrivingEvents { DriverId = driverIds[3].DriverID, Description = "Fueling", EventDate = DateTime.Now.AddDays(-7), AmountOut = 55, AmountIn = 0 },
                    new DrivingEvents { DriverId = driverIds[4].DriverID, Description = "Accident Repair", EventDate = DateTime.Now.AddDays(-8), AmountOut = 1000, AmountIn = 0 },
                    new DrivingEvents { DriverId = driverIds[5].DriverID, Description = "Fueling", EventDate = DateTime.Now.AddDays(-9), AmountOut = 45, AmountIn = 0 },
                    new DrivingEvents { DriverId = driverIds[6].DriverID, Description = "Car Wash", EventDate = DateTime.Now.AddDays(-10), AmountOut = 25, AmountIn = 0 },

                    // New events with incomes
                    new DrivingEvents { DriverId = driverIds[0].DriverID, Description = "Delivery Service", EventDate = DateTime.Now.AddDays(-11), AmountOut = 0, AmountIn = 500 },
                    new DrivingEvents { DriverId = driverIds[1].DriverID, Description = "Customer Pickup", EventDate = DateTime.Now.AddDays(-12), AmountOut = 0, AmountIn = 300 },
                    new DrivingEvents { DriverId = driverIds[2].DriverID, Description = "Rental Income", EventDate = DateTime.Now.AddDays(-13), AmountOut = 0, AmountIn = 800 },
                    new DrivingEvents { DriverId = driverIds[3].DriverID, Description = "Transport Fee", EventDate = DateTime.Now.AddDays(-14), AmountOut = 0, AmountIn = 400 },
                    new DrivingEvents { DriverId = driverIds[4].DriverID, Description = "Maintenance & Service Income", EventDate = DateTime.Now.AddDays(-15), AmountOut = 150, AmountIn = 700 },
                    new DrivingEvents { DriverId = driverIds[5].DriverID, Description = "Cargo Transport", EventDate = DateTime.Now.AddDays(-16), AmountOut = 50, AmountIn = 900 },
                    new DrivingEvents { DriverId = driverIds[6].DriverID, Description = "Special Delivery Service", EventDate = DateTime.Now.AddDays(-17), AmountOut = 0, AmountIn = 1000 },
                    new DrivingEvents { DriverId = driverIds[7].DriverID, Description = "Customer Transport", EventDate = DateTime.Now.AddDays(-18), AmountOut = 20, AmountIn = 600 },
                    new DrivingEvents { DriverId = driverIds[8].DriverID, Description = "VIP Transport Service", EventDate = DateTime.Now.AddDays(-19), AmountOut = 30, AmountIn = 1200 },
                    new DrivingEvents { DriverId = driverIds[9].DriverID, Description = "Equipment Rental", EventDate = DateTime.Now.AddDays(-20), AmountOut = 10, AmountIn = 400 }
                );
            }

            await context.SaveChangesAsync();
            Console.WriteLine("SeedData Initialization complete.");
        }
    }
}


using System.ComponentModel.DataAnnotations;

namespace DriverTracker.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeID { get; set; } // Unique ID for the employee

        [Required]
        [StringLength(100)]
        public string Name { get; set; } // Name of the employee

        [Required]
        [EmailAddress]
        public string Email { get; set; } // Email of the employee

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } // Password for authentication

        public string Role { get; set; } = "Employee"; // Role of the employee (e.g., Employee or Admin)

        // Navigation property for drivers managed by this employee
        public ICollection<Driver> Drivers { get; set; } = new List<Driver>(); // Drivers that this employee is responsible for
    }
}

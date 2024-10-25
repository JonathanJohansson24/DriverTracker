using System.ComponentModel.DataAnnotations;

namespace DriverTracker.Models
{
    public class Admin 
    {
        [Key]
        public int AdminID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; } = "Admin";
    }
}

using System.ComponentModel.DataAnnotations;

namespace DriverTracker.Models
{
    public class DrivingEvents
    {

        [Key]
        public int DriverEventId { get; set; } // Unique ID for the event

        [Required]
        [StringLength(200)]
        public string Description { get; set; } // Description of the event (e.g., "Fueling", "Car crash")

        [Required]
        public DateTime EventDate { get; set; } // Date of the event

        [Range(0, double.MaxValue)]
        public decimal AmountOut { get; set; } // Any cost associated with the event

        [Range(0, double.MaxValue)]
        public decimal AmountIn { get; set; } // Any income associated with the event (if applicable)

        // Foreign Key and Navigation Property for Driver
        [Required]
        public int DriverId { get; set; }

        public Driver Driver { get; set; }
    }
}

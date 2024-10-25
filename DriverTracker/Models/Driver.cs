using System.ComponentModel.DataAnnotations;

namespace DriverTracker.Models
{
        public class Driver
        {
        [Key]
        public int DriverID { get; set; } // Unique ID for the driver

        [Required]
        [StringLength(100)]
        public string DriverName { get; set; } // Name of the driver

        [Required]
        [StringLength(10)] // Adjust as per car registration number format
        public string CarReg { get; set; } // Car registration number

        // Navigation property for events
        public ICollection<DrivingEvents> Events { get; set; } = new List<DrivingEvents>(); // A collection of events related to the driver

        // Foreign key for Responsible Employee
        [Required]
        public int ResponsibleEmployeeId { get; set; }

        // Navigation property for Employee
        public Employee ResponsibleEmployee { get; set; }

        // Calculated properties for totals
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal TotalAmountOut => Events?.Sum(e => e.AmountOut) ?? 0; // Total amount spent

        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal TotalAmountIn => Events?.Sum(e => e.AmountIn) ?? 0; // Total income

    }
}

namespace DriverTracker.Models
{
    public class NotificationViewModel
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public string CarReg { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public string ResponsibleEmployee { get; set; }
        public decimal AmountIn { get; set; }
        public decimal AmountOut { get; set; }
    }
}

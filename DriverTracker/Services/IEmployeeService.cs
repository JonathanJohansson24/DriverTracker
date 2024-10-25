using DriverTracker.Data;
using DriverTracker.Models;

namespace DriverTracker.Services
{
    public interface IEmployeeService
    {
        Task AddDriverAsync(Driver driver);
        Task UpdateDriverAsync(Driver driver);
        Task DeleteDriverAsync(int driverId);
        Task<IEnumerable<Driver>> FilterDriversByNameAsync(string name);
        Task AddDriverEventAsync(int driverId, string description, decimal amountOut, decimal amountIn);
        Task<IEnumerable<DrivingEvents>> FilterDriverEventsByDateAsync(DateTime fromDate, DateTime toDate, int driverId);
        Task<IEnumerable<NotificationViewModel>> GetNotificationsForLast12HoursAsync();
    }

    public class EmployeeService : IEmployeeService
    {
        private readonly IDriverRepository _driverRepository;
        private readonly IDrivingEventRepository _eventRepository;

        public EmployeeService(IDriverRepository driverRepository, IDrivingEventRepository eventRepository)
        {
            _driverRepository = driverRepository;
            _eventRepository = eventRepository;
        }

        public async Task AddDriverAsync(Driver driver)
        {
            await _driverRepository.AddDriverAsync(driver);
        }

        public async Task UpdateDriverAsync(Driver driver)
        {
            await _driverRepository.UpdateDriverAsync(driver);
        }

        public async Task DeleteDriverAsync(int driverId)
        {
            await _driverRepository.DeleteDriverAsync(driverId);
        }

        public async Task<IEnumerable<Driver>> FilterDriversByNameAsync(string name)
        {
            return await _driverRepository.FilterDriversByNameAsync(name);
        }

        public async Task AddDriverEventAsync(int driverId, string description, decimal amountOut, decimal amountIn)
        {
            var driver = await _driverRepository.GetDriverByIdAsync(driverId);
            if (driver != null)
            {
                var driverEvent = new DrivingEvents
                {
                    DriverId = driverId,
                    Description = description,
                    EventDate = DateTime.Now,
                    AmountOut = amountOut,
                    AmountIn = amountIn
                };

                await _eventRepository.AddEventAsync(driverEvent);
            }
        }

        public async Task<IEnumerable<DrivingEvents>> FilterDriverEventsByDateAsync(DateTime fromDate, DateTime toDate, int driverId)
        {
            var driver = await _driverRepository.GetDriverByIdAsync(driverId);
            if (driver != null)
            {
                return await _eventRepository.GetEventsByDateRangeAsync(fromDate, toDate);
            }
            return Enumerable.Empty<DrivingEvents>();
        }
        public async Task<IEnumerable<NotificationViewModel>> GetNotificationsForLast12HoursAsync()
        {
            var events = await _eventRepository.GetEventsByDateRangeAsync(
                DateTime.Now.AddHours(-12),
                DateTime.Now
            );

            return events.Select(e => new NotificationViewModel
            {
                DriverId = e.DriverId,
                DriverName = e.Driver.DriverName,
                CarReg = e.Driver.CarReg,
                Description = e.Description,
                EventDate = e.EventDate,
                ResponsibleEmployee = e.Driver.ResponsibleEmployee.Name,
                AmountIn = e.AmountIn,
                AmountOut = e.AmountOut
            });
        }
    }

}

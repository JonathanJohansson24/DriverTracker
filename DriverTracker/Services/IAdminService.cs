using DriverTracker.Data;
using DriverTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DriverTracker.Services
{
    public interface IAdminService
    {
        // Employee management
        Task AddEmployeeAsync(Employee employee);
        Task UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(int employeeId);
        Task<Employee> GetEmployeeByIdAsync(int employeeId);
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();

        // Admin specific methods
        Task<IEnumerable<Admin>> GetAllAdminsAsync();
        Task<Admin> GetAdminByEmailAsync(string email);
        Task<IEnumerable<Driver>> GetDriversWithRecentActivityAsync();
        Task<IEnumerable<DrivingEvents>> FilterHistoryAsync(DateTime fromDate, DateTime toDate, string driverName, string employeeName);
        Task<IEnumerable<NotificationViewModel>> GetDetailedNotificationsForLast24HoursAsync();

        // Access to Employee service functionality
        IEmployeeService EmployeeService { get; }
    }

    public class AdminService : IAdminService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IDrivingEventRepository _eventRepository;
        private readonly AppDbContext _context;
        public IEmployeeService EmployeeService { get; }

        public AdminService(
            IEmployeeService employeeService,
            IEmployeeRepository employeeRepository,
            IDrivingEventRepository eventRepository,
            AppDbContext context)
        {
            EmployeeService = employeeService;
            _employeeRepository = employeeRepository;
            _eventRepository = eventRepository;
            _context = context;
        }

        // Employee management methods
        public async Task AddEmployeeAsync(Employee employee)
        {
            await _employeeRepository.AddEmployeeAsync(employee);
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            await _employeeRepository.UpdateEmployeeAsync(employee);
        }

        public async Task DeleteEmployeeAsync(int employeeId)
        {
            await _employeeRepository.DeleteEmployeeAsync(employeeId);
        }

        public async Task<Employee> GetEmployeeByIdAsync(int employeeId)
        {
            return await _employeeRepository.GetEmployeeByIdAsync(employeeId);
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            return await _employeeRepository.GetAllEmployeesAsync();
        }

        // Admin specific methods
        public async Task<IEnumerable<Admin>> GetAllAdminsAsync()
        {
            return await _context.Admins.ToListAsync();
        }
        public async Task<Admin> GetAdminByEmailAsync(string email)
        {
            return await _context.Admins.FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task<IEnumerable<Driver>> GetDriversWithRecentActivityAsync()
        {
            var events = await _eventRepository.GetEventsByDateRangeAsync(
                DateTime.Now.AddHours(-24),
                DateTime.Now
            );

            // Get unique driver IDs from recent events
            var driverIds = events.Select(e => e.DriverId).Distinct();

            // Fetch the complete driver information for these IDs
            var drivers = await _context.Drivers
                .Include(d => d.Events)
                .Include(d => d.ResponsibleEmployee)
                .Where(d => driverIds.Contains(d.DriverID))
                .ToListAsync();

            return drivers;
        }

        public async Task<IEnumerable<DrivingEvents>> FilterHistoryAsync(DateTime fromDate, DateTime toDate, string driverName, string employeeName)
        {
            var events = await _eventRepository.GetEventsByDateRangeAsync(fromDate, toDate);

            return events.Where(e =>
                (string.IsNullOrEmpty(driverName) || e.Driver.DriverName.Contains(driverName, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(employeeName) || e.Driver.ResponsibleEmployee.Name.Contains(employeeName, StringComparison.OrdinalIgnoreCase))
            );
        }

        public async Task<IEnumerable<NotificationViewModel>> GetDetailedNotificationsForLast24HoursAsync()
        {
            var events = await _eventRepository.GetEventsByDateRangeAsync(
                DateTime.Now.AddHours(-24),
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
            }).OrderByDescending(n => n.EventDate);
        }
    }
}
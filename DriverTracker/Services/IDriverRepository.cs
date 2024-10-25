using DriverTracker.Data;
using DriverTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace DriverTracker.Services
{
    public interface IDriverRepository
    {
        Task AddDriverAsync(Driver driver);
        Task UpdateDriverAsync(Driver driver);
        Task DeleteDriverAsync(int driverId);
        Task<Driver> GetDriverByIdAsync(int driverId);
        Task<IEnumerable<Driver>> GetAllDriversAsync();
        Task<IEnumerable<Driver>> FilterDriversByNameAsync(string name);
    }

    public class DriverRepository : IDriverRepository
    {
        private readonly AppDbContext _context;

        public DriverRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddDriverAsync(Driver driver)
        {
            await _context.Drivers.AddAsync(driver);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDriverAsync(Driver driver)
        {
            _context.Drivers.Update(driver);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDriverAsync(int driverId)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver != null)
            {
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new Exception($"Driver with ID {driverId} not found.");
            }
        }

        public async Task<Driver> GetDriverByIdAsync(int driverId)
        {
            return await _context.Drivers
                                 .Include(d => d.Events) // Load related events
                                 .Include(d => d.ResponsibleEmployee) // Load responsible employee
                                 .FirstOrDefaultAsync(d => d.DriverID == driverId);
        }

        public async Task<IEnumerable<Driver>> GetAllDriversAsync()
        {
            return await _context.Drivers
                .Include(d => d.Events) // Eager load events for each driver
                .ToListAsync();
        }

        public async Task<IEnumerable<Driver>> FilterDriversByNameAsync(string name)
        {
            return await _context.Drivers
                                 .Where(d => d.DriverName.Contains(name))
                                 .ToListAsync();
        }
    }
}

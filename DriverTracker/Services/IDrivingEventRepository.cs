using DriverTracker.Data;
using DriverTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace DriverTracker.Services
{
    public interface IDrivingEventRepository
    {
        Task AddEventAsync(DrivingEvents driverEvent);
        Task<IEnumerable<DrivingEvents>> GetEventsByDateRangeAsync(DateTime fromDate, DateTime toDate);
    }
    public class DrivingEventRepository : IDrivingEventRepository
    {
        private readonly AppDbContext _context;

        public DrivingEventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddEventAsync(DrivingEvents driverEvent)
        {
            await _context.Events.AddAsync(driverEvent);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<DrivingEvents>> GetEventsByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.Events
                .Where(e => e.EventDate >= fromDate && e.EventDate <= toDate)
                .Include(e => e.Driver) // Include driver information for filtering later
                .ThenInclude(d => d.ResponsibleEmployee) // Include responsible employee for filtering
                .ToListAsync();
        }
    }
}

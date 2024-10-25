using DriverTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DriverTracker.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<DrivingEvents> Events { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Ensures Identity tables are created

            // Add any custom model configurations or relationships here
            modelBuilder.Entity<DrivingEvents>()
                .Property(e => e.AmountIn)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<DrivingEvents>()
                .Property(e => e.AmountOut)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Driver>()
                .HasOne(d => d.ResponsibleEmployee)
                .WithMany(e => e.Drivers)
                .HasForeignKey(d => d.ResponsibleEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DrivingEvents>()
                .HasOne(e => e.Driver)
                .WithMany(d => d.Events)
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
    
}

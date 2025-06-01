using CF.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CF.API.DAL;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.Salary).HasPrecision(18, 2);
        });

        builder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "User" }
        );

        builder.Entity<DeviceType>().HasData(
            new DeviceType { Id = 1, Name = "Smartwatch" },
            new DeviceType { Id = 2, Name = "Personal Computer" },
            new DeviceType { Id = 3, Name = "Embedded Device" }
        );
    }

}
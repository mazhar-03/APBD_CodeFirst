using CF.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CF.API.DAL;

public class AppContext : DbContext
{
    public AppContext(DbContextOptions<AppContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
using CF.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CF.API.DAL;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceEmployee> DeviceEmployees { get; set; }
    public DbSet<DeviceType> DeviceTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Employee>()
            .HasOne(e => e.Account)
            .WithOne(a => a.Employee)
            .HasForeignKey<Account>(a => a.EmployeeId)
            .IsRequired();

        builder.Entity<Account>().ToTable("Account");
        builder.Entity<Role>().ToTable("Role");
        builder.Entity<Employee>().ToTable("Employee");
        builder.Entity<Person>().ToTable("Person");
        builder.Entity<Position>().ToTable("Position");
        builder.Entity<Device>().ToTable("Device");
        builder.Entity<DeviceType>().ToTable("DeviceType");
        builder.Entity<DeviceEmployee>().ToTable("DeviceEmployee");


        builder.Entity<Employee>(entity => { entity.Property(e => e.Salary).HasPrecision(18, 2); });

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
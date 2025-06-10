using CF.API.DAL;
using CF.API.DTOs;
using CF.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CF.API.Controllers;

[Route("api/employees")]
[ApiController]
public class EmployeeController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<Account> _passwordHasher = new();
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(AppDbContext context, ILogger<EmployeeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetEmployeeById(int id)
    {
        _logger.LogInformation($"Getting employee : {id}");
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning($"Username doesn't found : {username}");
            return Forbid();
        }
        var account = await _context.Accounts
            .Include(a => a.Employee)
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Username == username);

        if (account == null || account.Employee == null)
            return Forbid();

        var isAdmin = account.Role.Name == "Admin";
        if (!isAdmin && account.Employee.Id != id)
        {
            _logger.LogWarning($"Employee doesn't have an admin role : {account}");
            return Forbid();
        }

        var employee = await _context.Employees
            .Include(e => e.Person)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound($"Employee with ID {id} not found.");

        var dto = new SpecificEmployeeDto
        {
            Person = new PersonDto
            {
                PassportNumber = employee.Person.PassportNumber,
                FirstName = employee.Person.FirstName,
                MiddleName = employee.Person.MiddleName,
                LastName = employee.Person.LastName,
                PhoneNumber = employee.Person.PhoneNumber,
                Email = employee.Person.Email
            },
            Salary = employee.Salary,
            Position = employee.Position.Name,
            HireDate = employee.HireDate
        };

        return Ok(dto);
    }


    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateEmployeeById(int id, [FromBody] UpdateEmployeeDto dto)
    {
        _logger.LogInformation($"Updating employee : {id}");
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Forbid();

        var account = await _context.Accounts
            .Include(a => a.Employee)
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Username == username);

        if (account == null || account.Employee == null)
            return Forbid();

        var isAdmin = account.Role.Name == "Admin";

        if (!isAdmin && account.Employee.Id != id)
        {
            _logger.LogWarning($"Employee doesn't have an admin role : {account}");
            return Forbid();
        }

        var employee = await _context.Employees
            .Include(e => e.Person)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            _logger.LogWarning($"Employee with ID {id} not found.");
            return NotFound($"Employee with ID {id} not found.");
        }

        if (dto.Person != null)
        {
            employee.Person.PassportNumber = dto.Person.PassportNumber;
            employee.Person.FirstName = dto.Person.FirstName;
            employee.Person.MiddleName = dto.Person.MiddleName;
            employee.Person.LastName = dto.Person.LastName;
            employee.Person.PhoneNumber = dto.Person.PhoneNumber;
            employee.Person.Email = dto.Person.Email;
        }

        employee.Salary = dto.Salary;

        if (isAdmin && dto.PositionId.HasValue)
        {
            var position = await _context.Positions.FindAsync(dto.PositionId.Value);
            if (position == null)
            {
                _logger.LogWarning($"Position with ID {dto.PositionId.Value} not found.");
                return BadRequest($"Position with ID {dto.PositionId.Value} not found.");
            }

            employee.PositionId = dto.PositionId.Value;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Updated employee : {id}");
        return NoContent();
    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllEmployees()
    {
        try
        {
            _logger.LogInformation("Getting all employees");
            var employees = await _context.Employees
                .Include(e => e.Person)
                .Select(e => new AllEmployeesDto
                {
                    Id = e.Id,
                    FullName = $"{e.Person.FirstName} {e.Person.MiddleName ?? ""} {e.Person.LastName}"
                })
                .ToListAsync();

            return Ok(employees);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, "Error getting all employees");
            return BadRequest(e.Message);
        }
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
    {
        _logger.LogInformation("creating employee");

        var position = await _context.Positions.FindAsync(dto.PositionId);
        if (position == null)
        {
            _logger.LogWarning($"Position with ID {dto.PositionId} not found.");
            return BadRequest("Position not found.");
        }

        var person = new Person
        {
            FirstName = dto.Person.FirstName,
            MiddleName = dto.Person.MiddleName,
            LastName = dto.Person.LastName,
            PassportNumber = dto.Person.PassportNumber,
            PhoneNumber = dto.Person.PhoneNumber,
            Email = dto.Person.Email
        };

        var employee = new Employee
        {
            Person = person,
            Salary = dto.Salary,
            PositionId = dto.PositionId
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Employee created with id: {employee.Id}");

        return CreatedAtAction("GetEmployeeById", new { id = employee.Id }, new { employee.Id });
    }
}
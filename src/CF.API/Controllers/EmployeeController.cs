using CF.API.DAL;
using CF.API.DTOs;
using CF.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CF.API.Controllers;

[Route("api/employee")]
[ApiController]
public class EmployeeController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<Account> _passwordHasher = new();

    public EmployeeController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet("{id}")]
[Authorize]
public async Task<IActionResult> GetAccountById(int id)
{
    var account = await _context.Accounts
        .Include(a => a.Employee)
            .ThenInclude(e => e.Person)
        .Include(a => a.Employee)
            .ThenInclude(e => e.Position)
        .Include(a => a.Role)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (account == null)
        return NotFound("Account not found.");

    if (account.Employee == null)
        return NotFound("Employee data not found.");

    if (account.Employee.Person == null)
        return NotFound("Employee personal data not found.");

    if (account.Role == null)
        return NotFound("Role data not found.");

    var currentUsername = User.Identity?.Name;
    var isAdmin = User.IsInRole("Admin");

    if (!isAdmin && !string.Equals(account.Username, currentUsername, StringComparison.OrdinalIgnoreCase))
        return Forbid(); 

    var employeeDto = new EmployeeDto
    {
        Id = account.Employee.Id,
        FullName = $"{account.Employee.Person.FirstName} {account.Employee.Person.MiddleName} {account.Employee.Person.LastName}",
        Position = new PositionDto
        {
            Id = account.Employee.Position?.Id,
            Name = account.Employee.Position?.Name,
            MinExpYears = account.Employee.Position?.MinExpYears ?? 0
        },
        Person = new PersonDto
        {
            Id = account.Employee.Person.Id,
            FirstName = account.Employee.Person.FirstName,
            LastName = account.Employee.Person.LastName,
            Email = account.Employee.Person.Email,
            PhoneNumber = account.Employee.Person.PhoneNumber,
            PassportNumber = account.Employee.Person.PassportNumber
        }
    };

    var accountDto = new AccountDto
    {
        Id = account.Id,
        Username = account.Username,
        RoleName = account.Role.Name,
        Employee = employeeDto
    };

    return Ok(accountDto);
}

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateAccountById(int id, [FromBody] UpdateDto dto)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account == null)
            return NotFound($"Account with ID {id} not found.");

        var currentUsername = User.Identity?.Name;
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && !string.Equals(currentUsername, account.Username, StringComparison.OrdinalIgnoreCase))
            return Forbid(); 

        if (!string.IsNullOrEmpty(dto.Username) && dto.Username != account.Username)
        {
            if (await _context.Accounts.AnyAsync(a => a.Username == dto.Username && a.Id != id))
                return BadRequest("Username already taken.");

            account.Username = dto.Username;
        }

        if (!string.IsNullOrEmpty(dto.Password))
            account.Password = _passwordHasher.HashPassword(account, dto.Password);

        await _context.SaveChangesAsync();
        return NoContent();
    }

}
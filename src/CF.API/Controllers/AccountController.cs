using CF.API.DAL;
using CF.API.DTOs;
using CF.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CF.API.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<Account> _passwordHasher = new();

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _context.Accounts.AnyAsync(a => a.Username == dto.Username))
            return BadRequest("Username already exists.");

        var employee = await _context.Employees.FindAsync(dto.EmployeeId);
        if (employee == null)
            return BadRequest("Employee not found.");

        var role = await _context.Roles.FindAsync(dto.RoleId);
        if (role == null)
            return BadRequest("Role not found.");

        var employeeHasAccount = await _context.Accounts.AnyAsync(a => a.EmployeeId == dto.EmployeeId);
        if (employeeHasAccount)
            return BadRequest("Employee already has an account.");


        var account = new Account
        {
            Username = dto.Username,
            EmployeeId = dto.EmployeeId,
            RoleId = dto.RoleId
        };

        account.Password = _passwordHasher.HashPassword(account, dto.Password);

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetAccountById", new { id = account.Id }, account);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAccounts()
    {
        var accounts = await _context.Accounts
            .Select(a => new
            {
                a.Id,
                a.Username,
                a.Password
            })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAccountById(int id)
    {
        var account = await _context.Accounts
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Username,
                a.Password
            })
            .FirstOrDefaultAsync();

        if (account == null)
            return NotFound();

        return Ok(account);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAccount(RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _context.Accounts.AnyAsync(a => a.Username == dto.Username))
            return BadRequest("Username already exists.");

        var employee = await _context.Employees.FindAsync(dto.EmployeeId);
        if (employee == null)
            return BadRequest("Employee does not exist.");

        var role = await _context.Roles.FindAsync(dto.RoleId);
        if (role == null)
            return BadRequest("Role does not exist.");

        if (await _context.Accounts.AnyAsync(a => a.EmployeeId == dto.EmployeeId))
            return BadRequest("Employee already has an account.");

        var account = new Account
        {
            Username = dto.Username,
            EmployeeId = dto.EmployeeId,
            RoleId = dto.RoleId
        };

        account.Password = _passwordHasher.HashPassword(account, dto.Password);

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetAccountById", new { id = account.Id }, account);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAccount(int id, RegisterDto dto)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account == null)
            return NotFound();

        if (account.Username != dto.Username)
        {
            if (await _context.Accounts.AnyAsync(a => a.Username == dto.Username && a.Id != id))
                return BadRequest("Username already exists.");

            account.Username = dto.Username;
        }

        if (!string.IsNullOrEmpty(dto.Password))
            account.Password = _passwordHasher.HashPassword(account, dto.Password);

        var role = await _context.Roles.FindAsync(dto.RoleId);
        if (role == null)
            return BadRequest("Role does not exist.");

        account.RoleId = dto.RoleId;

        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account == null)
            return NotFound();

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Get own account info
    [HttpGet("myself")]
    [Authorize]
    public async Task<IActionResult> GetMyAccount()
    {
        var username = User.Identity?.Name;

        var account = await _context.Accounts
            .Include(a => a.Employee)
            .ThenInclude(e => e.Person)
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Username == username);

        if (account == null)
            return NotFound("Account not found.");

        if (account.Employee == null)
            return NotFound("Employee data not found.");

        if (account.Employee.Person == null)
            return NotFound("Employee personal data not found.");

        if (account.Role == null)
            return NotFound("Role data not found.");

        // Map to EmployeeDto
        var employeeDto = new EmployeeDto
        {
            Id = account.Employee.Id,
            FullName =
                $"{account.Employee.Person.FirstName} {account.Employee.Person.MiddleName} {account.Employee.Person.LastName}",
            Position = new PositionDto
            {
                Id = account.Employee.Position?.Id, // Safe check for null position
                Name = account.Employee.Position?.Name, // Safe check for null position
                MinExpYears = account.Employee.Position?.MinExpYears ?? 0 // Default to 0 if position is null
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

        // Map to AccountDto
        var accountDto = new AccountDto
        {
            Id = account.Id,
            Username = account.Username,
            RoleName = account.Role.Name,
            Employee = employeeDto
        };

        return Ok(accountDto);
    }

    [HttpPut("myself")]
    [Authorize]
    public async Task<IActionResult> UpdateMyAccount(UpdateDto dto)
    {
        var username = User.Identity?.Name;

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == username);
        if (account == null)
            return NotFound();

        if (!string.IsNullOrEmpty(dto.Username) && dto.Username != username)
        {
            if (await _context.Accounts.AnyAsync(a => a.Username == dto.Username && a.Id != account.Id))
                return BadRequest("Username already taken.");

            account.Username = dto.Username;
        }

        if (!string.IsNullOrEmpty(dto.Password))
            account.Password = _passwordHasher.HashPassword(account, dto.Password);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("whoami")]
    [Authorize]
    public IActionResult WhoAmI()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

        return Ok(new
        {
            User.Identity?.Name,
            User.Identity?.IsAuthenticated,
            Claims = claims
        });
    }
}
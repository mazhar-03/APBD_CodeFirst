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
    private readonly ILogger<AccountController> _logger;
    private readonly PasswordHasher<Account> _passwordHasher = new();

    public AccountController(AppDbContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAccounts()
    {
        var accounts = await _context.Accounts
            .Select(a => new
            {
                a.Id,
                a.Username
            })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetAccountById(int id)
    {
        _logger.LogInformation($"Getting Account {id}");
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Forbid();

        var account = await _context.Accounts
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (account == null)
        {
            _logger.LogInformation($"Account {id} not found");
            return NotFound("Account not found.");
        }

        var currentUser = await _context.Accounts
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Username == username);

        if (currentUser == null) return Forbid();

        var isAdmin = currentUser.Role.Name == "Admin";
        var isSelf = currentUser.Id == id;

        if (!isAdmin && !isSelf)
        {
            _logger.LogWarning($"Account {id} does not have admin rights");
            return Forbid();
        }

        var dto = new
        {
            account.Username,
            Role = account.Role.Name
        };
        _logger.LogInformation($"Account {id}: {dto}");
        return Ok(dto);
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAccount(RegisterDto dto)
    {
        _logger.LogInformation("Create new account");
        if (await _context.Accounts.AnyAsync(a => a.Username == dto.Username))
        {
            _logger.LogInformation($"Username {dto.Username} is already taken");
            return BadRequest("Username already exists.");
        }

        var employee = await _context.Employees.FindAsync(dto.EmployeeId);
        if (employee == null)
        {
            _logger.LogInformation($"Employee with id {dto.EmployeeId} not found");
            return BadRequest("Employee does not exist.");
        }
        
        var role = await _context.Roles.FindAsync(dto.RoleId);
        if (role == null)
        {
            _logger.LogInformation($"Role with id {dto.RoleId} not found");
            return BadRequest("Role does not exist.");
        }

        if (await _context.Accounts.AnyAsync(a => a.EmployeeId == dto.EmployeeId))
        {
            _logger.LogInformation($"Employee with id {dto.EmployeeId} is already has an account");
            return BadRequest("Employee already has an account.");
        }

        var account = new Account
        {
            Username = dto.Username,
            EmployeeId = dto.EmployeeId,
            RoleId = dto.RoleId
        };

        account.Password = _passwordHasher.HashPassword(account, dto.Password);

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Account created");
        
        return CreatedAtAction("GetAccountById", new { id = account.Id }, account);
    }


    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateAccountById(int id, [FromBody] RegisterDto dto)
    {
        _logger.LogInformation($"Update account with id: {id}");
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Forbid();

        var currentUser = await _context.Accounts
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Username == username);

        if (currentUser == null) return Forbid();

        var isAdmin = currentUser.Role.Name == "Admin";
        var isSelf = currentUser.Id == id;

        if (!isAdmin && !isSelf)
        {
            _logger.LogWarning($"User with id {id} does not have an admin role");
            return Forbid();
        }

        var targetAccount = await _context.Accounts.FindAsync(id);
        if (targetAccount == null)
        {
            _logger.LogWarning("Account {id} not found", id);
            return NotFound("Account not found.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != targetAccount.Username)
        {
            var exists = await _context.Accounts.AnyAsync(a => a.Username == dto.Username && a.Id != id);
            if (exists)
            {
                _logger.LogWarning($"User with id {dto.Username} already exists");
                return BadRequest("Username already exists.");
            }
            targetAccount.Username = dto.Username;
        }

        // Password change
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var hasher = new PasswordHasher<Account>();
            targetAccount.Password = hasher.HashPassword(targetAccount, dto.Password);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Account updated with id: {id}");
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        _logger.LogInformation($"Deleting account with id: {id}");
        var account = await _context.Accounts.FindAsync(id);
        if (account == null)
        {
            _logger.LogWarning("Account not found");
            return NotFound();
        }
        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Account deleted");
        return NoContent();
    }

//used for getting that value :
//NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" 
// [HttpGet("whoami")]
// [Authorize]
// public IActionResult WhoAmI()
// {
//     var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
//
//     return Ok(new
//     {
//         User.Identity?.Name,
//         User.Identity?.IsAuthenticated,
//         Claims = claims
//     });
// }
}
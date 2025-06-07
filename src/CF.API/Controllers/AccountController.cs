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
using CF.API.DAL;
using CF.API.DTOs;
using CF.API.Models;
using CF.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CF.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<Account> _passwordHasher = new();
    private readonly ITokenService _tokenService;

    public AuthController(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost]
    public async Task<IActionResult> Auth(LoginDto dto, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Username == dto.Username, cancellationToken);

        if (account == null)
            return Unauthorized("Invalid credentials");

        var verificationResult = _passwordHasher.VerifyHashedPassword(account, account.Password, dto.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
            return Unauthorized();

        var accessToken = new TokenResponseDTO
        {
            AccessToken = _tokenService.GenerateToken(account.Username,
                account.Role.Name)
        };
        return Ok(accessToken);
    }
}
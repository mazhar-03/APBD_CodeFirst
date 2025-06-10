using CF.API.DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CF.API.Controllers;

[ApiController]
[Route("/api/roles")]
public class RoleController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<RoleController> _logger;

    public RoleController(AppDbContext context, ILogger<RoleController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRoles()
    {
        _logger.LogInformation($"Getting roles.");
        var roles = await _context.Roles
            .Select(r => new
            {
                r.Id,
                r.Name
            }).ToListAsync();

        return Ok(roles);
    }
}
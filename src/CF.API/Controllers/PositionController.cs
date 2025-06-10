using CF.API.DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CF.API.Controllers;

[ApiController]
[Route("api/positions")]
public class PositionController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<PositionController> _logger;

    public PositionController(AppDbContext context, ILogger<PositionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/positions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPositions()
    {
        _logger.LogInformation($"Getting positions.");
        var positions = await _context.Positions
            .Select(p => new
            {
                p.Id,
                p.Name
            }).ToListAsync();

        return Ok(positions);
    }
}
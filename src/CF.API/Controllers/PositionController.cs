using CF.API.DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CF.API.Controllers;

[ApiController]
[Route("api/positions")]
public class PositionController: ControllerBase
{
    private readonly AppDbContext _context;

    public PositionController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet("/api/positions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPositions()
    {
        var positions = await _context.Positions
            .Select(p => new
            {
                p.Id,
                p.Name
            }).ToListAsync();

        return Ok(positions);
    }

}
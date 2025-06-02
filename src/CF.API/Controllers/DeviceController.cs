using System.Text.Json;
using CF.API.DAL;
using CF.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CF.API.Controllers;

[ApiController]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly AppDbContext _context;

    public DevicesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("myself")]
    [Authorize]
    public async Task<IActionResult> GetMyDevices()
    {
        var username = User.Identity?.Name;

        if (string.IsNullOrEmpty(username))
            return Unauthorized("User is not authenticated.");

        var account = await _context.Accounts
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Username == username);

        if (account == null || account.Employee == null)
            return NotFound("Account or Employee data not found.");

        var deviceEmployees = await _context.DeviceEmployees
            .Where(de => de.EmployeeId == account.Employee.Id)
            .Include(de => de.Device)
            .ThenInclude(d => d.DeviceType)
            .ToListAsync();

        var devices = deviceEmployees.Select(de => new DeviceDto
        {
            Id = de.Device.Id,
            Name = de.Device.Name,
            IsEnabled = de.Device.IsEnabled,
            AdditionalProperties =
                JsonDocument.Parse(de.Device.AdditionalProperties ?? "{}")
                    .RootElement,
            DeviceType = new DeviceTypeDto
            {
                Id = de.Device.DeviceType.Id,
                Name = de.Device.DeviceType.Name
            }
        }).ToList();

        return Ok(devices);
    }

    [HttpPut("{id}")]
[Authorize]
public async Task<IActionResult> UpdateMyDevice(int id, [FromBody] UpdateDeviceDto dto)
{
    var username = User.Identity?.Name;

    if (string.IsNullOrEmpty(username))
        return Unauthorized("User is not authenticated.");

    // Get the account linked with the username
    var account = await _context.Accounts
        .Include(a => a.Employee)
        .FirstOrDefaultAsync(a => a.Username == username);

    if (account == null || account.Employee == null)
        return NotFound("Account or Employee data not found.");

    // Find the device assigned to the user through DeviceEmployee relationship
    var deviceEmployee = await _context.DeviceEmployees
        .Include(de => de.Device) // Ensure the Device is loaded
        .FirstOrDefaultAsync(de => de.DeviceId == id && de.EmployeeId == account.Employee.Id);

    if (deviceEmployee == null)
        return NotFound("Device not found or not assigned to you.");

    // Access the Device from DeviceEmployee
    var device = deviceEmployee.Device;

    // Check if the device is null before accessing it
    if (device == null)
        return NotFound("Device data is null or problem about its linking with your device.");

    // Update device details if provided
    if (!string.IsNullOrEmpty(dto.Name))
        device.Name = dto.Name;

    if (dto.IsEnabled)
        device.IsEnabled = dto.IsEnabled;

    if (dto.AdditionalProperties.ValueKind == JsonValueKind.Null)
    {
        return BadRequest("AdditionalProperties cannot be null.");
    }
    else if (dto.AdditionalProperties.ValueKind != JsonValueKind.Null)
    {
        device.AdditionalProperties = dto.AdditionalProperties.GetRawText(); 
    }

    var type = await _context.DeviceTypes
        .FirstOrDefaultAsync(t => t.Name == dto.DeviceTypeName);

    if (type == null)
        return BadRequest($"Device type '{dto.DeviceTypeName}' not exist.");

    device.DeviceTypeId = type.Id;
    await _context.SaveChangesAsync();

    return NoContent(); 
}


}
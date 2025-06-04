using System.Text.Json;
using CF.API.DAL;
using CF.API.DTOs;
using CF.API.Models;
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

    [HttpPut("myself/{id}")]
[Authorize]
public async Task<IActionResult> UpdateMyDevice(int id, [FromBody] UpdateDeviceDto dto)
{
    var username = User.Identity?.Name;

    if (string.IsNullOrEmpty(username))
        return Unauthorized("User is not authenticated.");

    var account = await _context.Accounts
        .Include(a => a.Employee)
        .FirstOrDefaultAsync(a => a.Username == username);

    if (account == null || account.Employee == null)
        return NotFound("Account or Employee data not found.");

    var deviceEmployee = await _context.DeviceEmployees
        .Include(de => de.Device)
        .FirstOrDefaultAsync(de => de.DeviceId == id && de.EmployeeId == account.Employee.Id);

    if (deviceEmployee == null)
        return NotFound("Device not found or not assigned to you.");

    var device = deviceEmployee.Device;

    if (device == null)
        return NotFound("Device data is null or problem about its linking with your device.");

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

    [HttpGet]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetAllDevices()
{
    try
    {
        var devices = await _context.Devices
            .Select(d => new {d.Id, d.Name}).ToListAsync();

        return Ok(devices);
    }
    catch (Exception ex)
    {
        return Problem(ex.Message);
    }
}

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeviceById(int id)
    {
        try
        {
            var device = await _context.Devices
                .Include(d => d.DeviceType)
                .Include(d => d.DeviceEmployees)
                .ThenInclude(de => de.Employee)
                .ThenInclude(e => e.Person)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null)
                return NotFound($"Device {id} not found");

            var additionalJson = JsonDocument.Parse(device.AdditionalProperties ?? "{}").RootElement;
            var currentAssignment = device.DeviceEmployees
                .FirstOrDefault(de => de.ReturnDate == null);

            CurrentUserDTO? currentUser = null;
            if (currentAssignment != null && currentAssignment.Employee?.Person != null)
            {
                var person = currentAssignment.Employee.Person;
                currentUser = new CurrentUserDTO
                {
                    Id = currentAssignment.EmployeeId,
                    Name = $"{person.FirstName} {person.MiddleName} {person.LastName}"
                };
            }

            var dto = new DeviceDto
            {
                Id = device.Id,
                Name = device.Name,
                IsEnabled = device.IsEnabled,
                AdditionalProperties = additionalJson,
                DeviceType = new DeviceTypeDto
                {
                    Id = device.DeviceType.Id,
                    Name = device.DeviceType.Name
                }
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpPost]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceDto dev)
{
    try
    {
        if (dev.AdditionalProperties.ValueKind == JsonValueKind.Null)
            return BadRequest("AdditionalProperties cannot be null");

        var type = await _context.DeviceTypes
            .SingleOrDefaultAsync(t => t.Name == dev.DeviceTypeName);

        if (type == null)
            return BadRequest($"Unknown device type '{dev.DeviceTypeName}'");

        var device = new Device
        {
            Name = dev.Name,
            DeviceTypeId = type.Id,
            IsEnabled = dev.IsEnabled,
            AdditionalProperties = dev.AdditionalProperties.GetRawText()  
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        var returnedDto = new
        {
            id = device.Id,
            name = device.Name,
            deviceTypeName = type.Name,
            isEnabled = device.IsEnabled,
            additionalProperties = JsonDocument.Parse(device.AdditionalProperties ?? "{}").RootElement
        };

        return Created($"/api/devices/{device.Id}", returnedDto);
    }
    catch (Exception ex)
    {
        return Problem(ex.Message);
    }
}

[HttpPut("{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> UpdateDevice(int id, [FromBody] CreateDeviceDto dto)
{
    try
    {
        var device = await _context.Devices.FindAsync(id);
        if (device == null)
            return NotFound($"Device {id} not found");

        var type = await _context.DeviceTypes
            .SingleOrDefaultAsync(t => t.Name == dto.DeviceTypeName);
        if (type == null)
            return BadRequest($"Unknown device type '{dto.DeviceTypeName}'");

        device.Name = dto.Name;
        device.DeviceTypeId = type.Id;
        device.IsEnabled = dto.IsEnabled;
        device.AdditionalProperties = dto.AdditionalProperties.GetRawText();  

        await _context.SaveChangesAsync();
        return NoContent();
    }
    catch (Exception ex)
    {
        return Problem(ex.Message);
    }
}

[HttpDelete("{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> DeleteDevice(int id)
{
    try
    {
        var device = await _context.Devices.FindAsync(id);
        if (device == null)
            return NotFound($"Device {id} not found");

        var isAssigned = await _context.DeviceEmployees
            .AnyAsync(de => de.DeviceId == id);
        if (isAssigned)
            return BadRequest($"Cannot delete device {id} because it is associated with an employee");

        _context.Devices.Remove(device);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    catch (Exception ex)
    {
        return Problem("Error deleting device: " + ex.Message);
    }
}



}
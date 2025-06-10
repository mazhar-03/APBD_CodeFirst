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
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(AppDbContext context, ILogger<DevicesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllDevices()
    {
        try
        {
            _logger.LogInformation("Getting all devices");
            var devices = await _context.Devices
                .Select(d => new { d.Id, d.Name }).ToListAsync();

            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "Error while getting all devices");
            return Problem(ex.Message);
        }
    }

    //just Authorize cause user can see their own devices
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetDeviceById(int id)
    {
        _logger.LogInformation($"GetDeviceById started for device ID: {id}");

        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("Forbidden: User.Identity.Name is null.");
            return Forbid();
        }

        var account = await _context.Accounts
            .Include(a => a.Employee)
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Username == username);

        if (account == null || account.Employee == null)
        {
            _logger.LogWarning($"Forbidden: No account or employee for username: {username}");
            return Forbid();
        }

        var isAdmin = account.Role.Name == "Admin";

        if (!isAdmin)
        {
            var assigned = await _context.DeviceEmployees
                .AnyAsync(de => de.DeviceId == id && de.EmployeeId == account.Employee.Id && de.ReturnDate == null);

            if (!assigned)
            {
                _logger.LogWarning($"Forbidden: User '{username}' tried to access unassigned device ID: {id}");
                return Forbid();
            }
        }

        try
        {
            var device = await _context.Devices
                .Include(d => d.DeviceType)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null)
            {
                _logger.LogWarning($"Device not found with ID: {id}");
                return NotFound($"Device {id} not found");
            }

            var additionalJson = JsonDocument.Parse(device.AdditionalProperties ?? "{}").RootElement;

            var dto = new GetSpecificDeviceDto
            {
                Name = device.Name,
                IsEnabled = device.IsEnabled,
                AdditionalProperties = additionalJson,
                Type = device.DeviceType.Name
            };

            _logger.LogInformation($"Successfully returned device {id} to user '{username}'");
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception while fetching device ID: {id}");
            return Problem("Unexpected error occurred.");
        }
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceDto dev)
    {
        try
        {
            _logger.LogInformation("Creating new device");
            if (dev.AdditionalProperties.ValueKind == JsonValueKind.Null)
            {
                _logger.LogWarning("Adding additional properties to device is null");
                return BadRequest("AdditionalProperties cannot be null");
            }

            var type = await _context.DeviceTypes
                .SingleOrDefaultAsync(t => t.Id == dev.TypeId);
            if (type == null)
            {
                _logger.LogWarning($"Device type with id: {dev.TypeId} not found");
                return BadRequest($"Unknown device type for ID '{dev.TypeId}'");
            }

            var device = new Device
            {
                Name = dev.Name,
                DeviceTypeId = type.Id,
                IsEnabled = dev.IsEnabled,
                AdditionalProperties = dev.AdditionalProperties.GetRawText()
            };

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Added device with id: {device.Id}");

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
            _logger.LogError(ex.Message, "Error while creating new device.");
            return Problem(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateDevice(int id, [FromBody] CreateDeviceDto dto)
    {
        _logger.LogInformation($"UpdateDevice started for ID: {id}");

        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("Forbidden: User.Identity.Name is null.");
            return Forbid();
        }

        var account = await _context.Accounts
            .Include(a => a.Employee)
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Username == username);

        if (account == null || account.Employee == null)
        {
            _logger.LogWarning($"Forbidden: No account or employee for username: {username}");
            return Forbid();
        }

        var isAdmin = account.Role.Name == "Admin";

        if (!isAdmin)
        {
            var assigned = await _context.DeviceEmployees
                .AnyAsync(de => de.DeviceId == id && de.EmployeeId == account.Employee.Id && de.ReturnDate == null);

            if (!assigned)
            {
                _logger.LogWarning($"Forbidden: User '{username}' tried to update unassigned device ID: {id}");
                return Forbid();
            }
        }

        try
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                _logger.LogWarning($"Device with ID {id} not found.");
                return NotFound($"Device {id} not found");
            }

            var type = await _context.DeviceTypes
                .SingleOrDefaultAsync(t => t.Id == dto.TypeId);
            if (type == null)
            {
                _logger.LogWarning($"Device type with ID {dto.TypeId} not found.");
                return BadRequest($"Unknown device type '{dto.TypeId}'");
            }

            device.Name = dto.Name;
            device.DeviceTypeId = type.Id;
            device.IsEnabled = dto.IsEnabled;
            device.AdditionalProperties = dto.AdditionalProperties.GetRawText();

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Device ID {id} updated successfully by user '{username}'");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while updating device ID: {id}");
            return Problem("Unexpected error occurred.");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDevice(int id)
    {
        try
        {
            _logger.LogInformation($"Deleting device with id: {id}");

            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                _logger.LogWarning($"Device with id: {id} not found");
                return NotFound($"Device {id} not found");
            }

            var isAssigned = await _context.DeviceEmployees
                .AnyAsync(de => de.DeviceId == id);
            if (isAssigned)
            {
                _logger.LogWarning($"Device with id: {id} still assigned to device");
                return BadRequest($"Cannot delete device {id} because it is associated with an employee");
            }

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deleted device with id: {id}");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, $"Error while deleting device with id: {id}");
            return Problem("Error deleting device: " + ex.Message);
        }
    }

    [HttpGet("types")]
    [Authorize]
    public async Task<IActionResult> GetDeviceTypes()
    {
        var types = await _context.DeviceTypes
            .Select(t => new
            {
                t.Id,
                t.Name
            }).ToListAsync();

        return Ok(types);
    }
}
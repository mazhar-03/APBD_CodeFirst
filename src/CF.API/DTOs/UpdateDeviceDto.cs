using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace CF.API.DTOs;

public class UpdateDeviceDto
{
    [Required] public string Name { get; set; } = null!;

    [Required] public string DeviceTypeName { get; set; } = null!;

    [Required] public bool IsEnabled { get; set; }

    [Required] 
    public JsonElement AdditionalProperties { get; set; } 
}
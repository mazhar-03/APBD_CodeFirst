using System.Text.Json;

namespace CF.API.DTOs;

public class GetSpecificDeviceDto
{
    public string Name { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public JsonElement AdditionalProperties { get; set; }

    public string Type { get; set; }
}
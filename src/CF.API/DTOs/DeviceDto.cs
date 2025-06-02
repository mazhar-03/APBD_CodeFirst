using System.Text.Json;

namespace CF.API.DTOs;

public class DeviceDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsEnabled { get; set; }
    public JsonElement AdditionalProperties { get; set; }
    public DeviceTypeDto DeviceType { get; set; }
}
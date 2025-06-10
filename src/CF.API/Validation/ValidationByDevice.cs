namespace CF.API.Validation;

public class ValidationByDevice
{
    public string Type { get; set; }
    public string PreRequestName { get; set; }
    public string PreRequestValue { get; set; }
    public List<string> Rules { get; set; }
}
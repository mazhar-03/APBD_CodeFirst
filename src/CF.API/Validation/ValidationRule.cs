using System.Text.Json;

namespace CF.API.Validation;

public class ValidationRule
{
    public string ParamName { get; set; }
    public string Regex { get; set; }
    public List<string> AllowedValues { get; set; }
}
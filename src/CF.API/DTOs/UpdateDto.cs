using System.ComponentModel.DataAnnotations;

namespace CF.API.DTOs;

public class UpdateDto
{
    [RegularExpression(@"^[^\d].*$", ErrorMessage = "Username must not start with a number.")]
    public string? Username { get; set; }

    [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).+$",
        ErrorMessage = "Password must contain lowercase, uppercase, number and symbol.")]
    public string? Password { get; set; }
}
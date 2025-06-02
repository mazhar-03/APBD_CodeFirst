using System.ComponentModel.DataAnnotations;

namespace CF.API.DTOs;

public class RegisterDto
{
    [Required]
    [RegularExpression(@"^[^\d].*$", ErrorMessage = "Username shouldn’t start with numbers.")]
    public string Username { get; set; }

    [Required]
    [MinLength(12, ErrorMessage = "Password should have length at least 12.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).+$",
        ErrorMessage = "Password should have at least one small letter, one capital letter, one number and one symbol")]
    public string Password { get; set; }

    [Required] public int EmployeeId { get; set; }

    public int RoleId { get; set; }
}
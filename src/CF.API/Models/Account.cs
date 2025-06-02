using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CF.API.Models;

public class Account
{
    public int Id { get; set; }

    [Required]
    [RegularExpression(@"^[^\d].*$")]
    public string Username { get; set; }

    [Required] public string Password { get; set; }

    public int EmployeeId { get; set; }

    [JsonIgnore] public Employee Employee { get; set; }

    public int RoleId { get; set; }
    public Role Role { get; set; }
}
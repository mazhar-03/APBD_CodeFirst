namespace CF.API.DTOs;

public class AccountDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string RoleName { get; set; }
    public EmployeeDto Employee { get; set; }
}
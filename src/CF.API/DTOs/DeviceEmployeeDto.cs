namespace CF.API.DTOs;

public class DeviceEmployeeDto
{
    public int Id { get; set; }
    public DeviceDto Device { get; set; }
    public EmployeeDto Employee { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
}
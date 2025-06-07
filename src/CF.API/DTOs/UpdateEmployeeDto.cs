namespace CF.API.DTOs;

public class UpdateEmployeeDto
{
    public PersonDto? Person { get; set; }
    public decimal Salary { get; set; }
    public int? PositionId { get; set; } 
}

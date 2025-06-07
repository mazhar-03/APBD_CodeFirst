namespace CF.API.DTOs;

public class CreateEmployeeDto
{
    public CreatePersonDto Person { get; set; } = null!;
    public decimal Salary { get; set; }
    public int PositionId { get; set; }
}
namespace CF.API.DTOs;

public class SpecificEmployeeDto
{
    public PersonDto Person { get; set; } = null!;
    public decimal Salary { get; set; }
    public string Position { get; set; } = null!;
    public DateTime HireDate { get; set; }
}
namespace CF.API.Models;

public class Role
{
    public int Id { get; set; }
    
    public string Name { get; set; }  
    
    public ICollection<Account> Accounts { get; set; }
}
namespace CF.API.Services;

public interface ITokenService
{
    string GenerateToken(string username, string role);
}
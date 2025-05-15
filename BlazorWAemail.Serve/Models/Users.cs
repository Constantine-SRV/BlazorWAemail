namespace BlazorWAemail.Server.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }

    public List<UserToken> Tokens { get; set; }
    public List<UserRole> UserRoles { get; set; }
}

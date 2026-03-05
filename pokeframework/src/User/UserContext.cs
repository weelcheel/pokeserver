namespace PokeFramework.User;

public class UserContext(string connectionId)
{
    public string? UserId { get; set; }
    public string ConnectionId { get; } = connectionId;
}
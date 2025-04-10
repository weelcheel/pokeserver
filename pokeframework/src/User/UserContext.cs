namespace PokeFramework.User;

public class UserContext(string serverId)
{
    public string? UserId { get; set; }
    public string ServerId { get; } = serverId;
}
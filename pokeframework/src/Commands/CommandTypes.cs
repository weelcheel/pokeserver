namespace PokeFramework.Commands;

public enum CommandType : byte
{
    Authenticate,
    AuthenticateResult,
    AuthenticateUserPass,
    JoinMap,
    JoinMapResult,
    Move,
    GameState,
    PlayerMovement,
    Disconnect,
}

public class Command(CommandType commandType, string connectionId, string? userId = null, byte[]? commandParams = null)
{
    public CommandType CommandType { get; } = commandType;
    public byte[]? CommandParams { get; } = commandParams;
    public string ConnectionId { get; } = connectionId;
    public string? UserId { get; } = userId;
}

public static class Constants
{
    public const uint PacketMagic = 0x07100420;
    public const uint Success = 0xAAAA0710;
    public const uint Failure = 0xFFFF0710;
}
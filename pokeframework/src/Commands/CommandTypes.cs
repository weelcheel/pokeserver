namespace PokeFramework.Commands;

public enum CommandType : byte
{
    Authenticate,
    AuthenticateResult
}

public class Command(CommandType commandType, string connectionId, byte[] commandParams)
{
    public CommandType CommandType { get; } = commandType;
    public byte[] CommandParams { get; } = commandParams;
    public string ConnectionId { get; } = connectionId;
}

public static class Constants
{
    public const uint PacketMagic = 0x07100420;
    public const uint Success = 0xAAAA0710;
    public const uint Failure = 0xFFFF0710;
}
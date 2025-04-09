namespace PokeServer.Game;

public enum PacketCommandType : byte
{
    Authenticate,
    AuthenticateResult
}

public class Command(PacketCommandType commandType, byte commandParamsSize, byte[] commandParams)
{
    public PacketCommandType CommandType { get; set; } = commandType;
    public byte CommandParamsSize { get; set; } = commandParamsSize;
    public byte[] CommandParams { get; set; } = commandParams;
}

public class Packet
{
    
}

public static class Constants
{
    public const uint PacketMagic = 0x07100420;
    
    public const uint Success = 0xAAAA0710;
}
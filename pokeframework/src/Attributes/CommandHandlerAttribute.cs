using PokeFramework.Commands;

namespace PokeFramework.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CommandHandlerAttribute(params CommandType[] commandTypes) : Attribute
{
    public CommandType[] CommandTypes { get; } = commandTypes;
}

[AttributeUsage(AttributeTargets.Method)]
public class CommandHandlerAuthenticatedAttribute(params CommandType[] commandTypes)
    : CommandHandlerAttribute(commandTypes)
{
}
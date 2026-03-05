namespace PokeWorld.Trainer;

public class TrainerLocation
{
    public short X { get; init; }
    public short Y { get; init; }
    public byte Action { get; init; }
    public byte CurrentElevation { get; init; }
    public byte FacingDirection { get; init; }
}
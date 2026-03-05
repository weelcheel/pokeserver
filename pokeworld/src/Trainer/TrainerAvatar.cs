namespace PokeWorld.Trainer;

public class TrainerAvatar(string userId, byte gameUserId)
{
    public string UserId { get; } = userId;
    public byte GameUserId { get; } = gameUserId;

    public TrainerLocation Location { get; set; } = new();
    public bool HasPosition { get; set; }
}
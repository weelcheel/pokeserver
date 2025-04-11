namespace PokeWorld.Trainer;

public class TrainerAvatar(string userId, byte gameUserId)
{
    public string UserId { get; } = userId;
    public byte GameUserId { get; } = gameUserId;

    public TrainerMovement Movement { get; private set; } = new();
    
    public void UpdateMovement(TrainerMovement newMovement)
    {
        Movement = newMovement;
    }
}
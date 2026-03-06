using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PokeEntities.Postgres.Entities;

/// <summary>
/// User on the platform.
/// </summary>
[Table("user", Schema = "user")]
[Index(nameof(Username), IsUnique = true)]
public class User
{
    /// <summary>
    /// The player's platform/user id. This is used as the primary key.
    /// </summary>
    public Guid UserId { get; set; } = Guid.Empty;
    
    /// <summary>
    /// Username used to login for this user.
    /// </summary>
    [MaxLength(255)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the password the user uses to authenticate.
    /// </summary>
    [MaxLength(255)]
    public string Password { get; set; } = string.Empty;
}


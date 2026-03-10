using System.ComponentModel.DataAnnotations;

namespace QuizGame.Models;

public class Player
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}

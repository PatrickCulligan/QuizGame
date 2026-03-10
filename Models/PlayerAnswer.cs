namespace QuizGame.Models;

public class PlayerAnswer
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public int QuestionId { get; set; }
    public int PlayerId { get; set; }
    public string SelectedAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public double ResponseSeconds { get; set; }
    public int AwardedPoints { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}

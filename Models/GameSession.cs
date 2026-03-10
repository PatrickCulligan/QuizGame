namespace QuizGame.Models;

public enum SessionStatus
{
    Lobby = 0,
    QuestionLive = 1,
    QuestionClosed = 2,
    Finished = 3
}

public class GameSession
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = default!;
    public string Pin { get; set; } = string.Empty;
    public SessionStatus Status { get; set; } = SessionStatus.Lobby;
    public int CurrentQuestionIndex { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? QuestionStartedAtUtc { get; set; }
    public DateTime? QuestionEndsAtUtc { get; set; }
}

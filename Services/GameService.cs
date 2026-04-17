using Microsoft.EntityFrameworkCore;
using QuizGame.Data;
using QuizGame.Models;

namespace QuizGame.Services;

public class GameService
{
    private readonly AppDbContext _db;
    public GameService(AppDbContext db) => _db = db;

    public async Task<GameSession> CreateSessionAsync(int quizId)
    {
        var pin = await GeneratePinAsync();
        var session = new GameSession
        {
            QuizId = quizId,
            Pin = pin,
            Status = SessionStatus.Lobby
        };
        _db.GameSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task<string> GeneratePinAsync()
    {
        var random = new Random();
        string pin;
        do { pin = random.Next(100000, 999999).ToString(); }
        while (await _db.GameSessions.AnyAsync(x => x.Pin == pin));
        return pin;
    }

    public async Task<Question?> GetCurrentQuestionAsync(int sessionId)
    {
        var session = await _db.GameSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null || session.CurrentQuestionIndex < 1) return null;

        return await _db.Questions
            .Where(q => q.QuizId == session.QuizId && q.OrderIndex == session.CurrentQuestionIndex)
            .FirstOrDefaultAsync();
    }

    public async Task<(bool ok, string message, Player? player, GameSession? session)> JoinAsync(string pin, string username)
    {
        var session = await _db.GameSessions.FirstOrDefaultAsync(s => s.Pin == pin);
        if (session is null) return (false, "Game PIN not found.", null, null);
        if (session.Status != SessionStatus.Lobby) return (false, "Game already started.", null, session);

        var trimmed = username.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return (false, "Username is required.", null, session);

        var player = await _db.Players.FirstOrDefaultAsync(p => p.GameSessionId == session.Id && p.Username == trimmed);
        if (player is null)
        {
            // new players are not admitted by default
            player = new Player { GameSessionId = session.Id, Username = trimmed, JoinedAtUtc = DateTime.UtcNow, IsAdmitted = false };
            _db.Players.Add(player);
            await _db.SaveChangesAsync();
        }

        return (true, "", player, session);
    }

    public async Task SubmitAnswerAsync(int sessionId, int playerId, string selectedAnswer)
    {
        var session = await _db.GameSessions.Include(s => s.Quiz).FirstAsync(s => s.Id == sessionId);
        var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == playerId && p.GameSessionId == sessionId);
        if (player is null || !player.IsAdmitted) return;

        if (session.Status != SessionStatus.QuestionLive) return;
        if (session.QuestionEndsAtUtc.HasValue && DateTime.UtcNow > session.QuestionEndsAtUtc.Value) return;

        var question = await _db.Questions.FirstAsync(q => q.QuizId == session.QuizId && q.OrderIndex == session.CurrentQuestionIndex);

        var exists = await _db.PlayerAnswers.AnyAsync(a => a.PlayerId == playerId && a.QuestionId == question.Id);
        if (exists) return;

        var elapsed = 0.0;
        if (session.QuestionStartedAtUtc.HasValue)
            elapsed = Math.Max(0, (DateTime.UtcNow - session.QuestionStartedAtUtc.Value).TotalSeconds);

        elapsed = Math.Min(elapsed, session.Quiz.SecondsPerQuestion);
        var isCorrect = string.Equals(selectedAnswer?.Trim(), question.CorrectAnswer, StringComparison.OrdinalIgnoreCase);

        var points = 0;
        if (isCorrect)
        {
            var speedBonus = Math.Max(0, session.Quiz.SecondsPerQuestion - elapsed);
            points = 500 + (int)Math.Round(speedBonus * 30);
        }

        _db.PlayerAnswers.Add(new PlayerAnswer
        {
            GameSessionId = sessionId,
            PlayerId = playerId,
            QuestionId = question.Id,
            SelectedAnswer = selectedAnswer ?? string.Empty,
            IsCorrect = isCorrect,
            ResponseSeconds = elapsed,
            AwardedPoints = points,
            SubmittedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}

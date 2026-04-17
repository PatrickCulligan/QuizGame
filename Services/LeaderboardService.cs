using Microsoft.EntityFrameworkCore;
using QuizGame.Data;

namespace QuizGame.Services;

public class LeaderboardService
{
    private readonly AppDbContext _db;
    public LeaderboardService(AppDbContext db) => _db = db;

    public async Task<List<LeaderboardRow>> GetLeaderboardAsync(int sessionId)
    {
        var players = await _db.Players.Where(p => p.GameSessionId == sessionId && p.IsAdmitted).ToListAsync();
        var answers = await _db.PlayerAnswers.Where(a => a.GameSessionId == sessionId).ToListAsync();

        return players.Select(p => new LeaderboardRow
        {
            PlayerId = p.Id,
            Username = p.Username,
            Score = answers.Where(a => a.PlayerId == p.Id).Sum(a => a.AwardedPoints),
            CorrectAnswers = answers.Count(a => a.PlayerId == p.Id && a.IsCorrect),
            TotalResponseSeconds = answers.Where(a => a.PlayerId == p.Id).Sum(a => a.ResponseSeconds)
        })
        .OrderByDescending(x => x.Score)
        .ThenByDescending(x => x.CorrectAnswers)
        .ThenBy(x => x.TotalResponseSeconds)
        .ToList();
    }
}

public class LeaderboardRow
{
    public int PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
    public double TotalResponseSeconds { get; set; }
}

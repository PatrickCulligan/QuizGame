using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizGame.Data;
using QuizGame.Models;
using QuizGame.Services;

namespace QuizGame.Pages;

public class ResultsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly LeaderboardService _leaderboardService;

    public ResultsModel(AppDbContext db, LeaderboardService leaderboardService)
    {
        _db = db;
        _leaderboardService = leaderboardService;
    }

    [BindProperty(SupportsGet = true)] public int? SessionId { get; set; }
    [BindProperty(SupportsGet = true)] public int? PlayerId { get; set; }

    public GameSession? Session { get; set; }
    public List<LeaderboardRow> Rows { get; set; } = new();

    public async Task OnGetAsync()
    {
        Session = SessionId.HasValue
            ? await _db.GameSessions.FirstOrDefaultAsync(x => x.Id == SessionId.Value)
            : await _db.GameSessions.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

        if (Session is not null)
            Rows = await _leaderboardService.GetLeaderboardAsync(Session.Id);
    }
}

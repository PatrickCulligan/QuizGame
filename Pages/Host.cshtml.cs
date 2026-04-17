using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizGame.Data;
using QuizGame.Hubs;
using QuizGame.Models;
using QuizGame.Services;

namespace QuizGame.Pages;

[Authorize(Roles = "Admin")]
public class HostModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly GameService _gameService;
    private readonly IHubContext<GameHub> _hub;

    public HostModel(AppDbContext db, GameService gameService, IHubContext<GameHub> hub)
    {
        _db = db;
        _gameService = gameService;
        _hub = hub;
    }

    [BindProperty(SupportsGet = true)]
    public int? SessionId { get; set; }

    public GameSession? Session { get; set; }
    public Quiz? Quiz { get; set; }
    public Question? CurrentQuestion { get; set; }
    public List<Player> Players { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostStartQuestionAsync()
    {
        await LoadAsync();
        if (Session is null || Quiz is null) return RedirectToPage();

        if (Session.Status == SessionStatus.Lobby)
        {
            Session.CurrentQuestionIndex = 1;
        }
        else if (Session.Status == SessionStatus.QuestionClosed)
        {
            if (Session.CurrentQuestionIndex >= Quiz.QuestionCount)
                return RedirectToPage(new { sessionId = Session.Id });

            Session.CurrentQuestionIndex += 1;
        }
        else
        {
            return RedirectToPage(new { sessionId = Session.Id });
        }

        Session.Status = SessionStatus.QuestionLive;
        Session.QuestionStartedAtUtc = DateTime.UtcNow;
        Session.QuestionEndsAtUtc = DateTime.UtcNow.AddSeconds(Quiz.SecondsPerQuestion);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group($"session-{Session.Id}").SendAsync("SessionUpdated");
        return RedirectToPage(new { sessionId = Session.Id });
    }

    public async Task<IActionResult> OnPostCloseQuestionAsync()
    {
        await LoadAsync();
        if (Session is null) return RedirectToPage();

        Session.Status = SessionStatus.QuestionClosed;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group($"session-{Session.Id}").SendAsync("SessionUpdated");
        return RedirectToPage(new { sessionId = Session.Id });
    }

    public async Task<IActionResult> OnPostFinishAsync()
    {
        await LoadAsync();
        if (Session is null) return RedirectToPage();

        Session.Status = SessionStatus.Finished;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group($"session-{Session.Id}").SendAsync("SessionUpdated");
        return RedirectToPage("/Results", new { sessionId = Session.Id });
    }

    // New: admit a player
    public async Task<IActionResult> OnPostAdmitAsync(int playerId, int sessionId)
    {
        var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == playerId && p.GameSessionId == sessionId);
        if (player is null) return RedirectToPage(new { sessionId = sessionId });

        player.IsAdmitted = true;
        await _db.SaveChangesAsync();

        // Notify the session group so lobby pages reload and show admitted state
        await _hub.Clients.Group($"session-{sessionId}").SendAsync("PlayersUpdated");

        return RedirectToPage(new { sessionId = sessionId });
    }

    private async Task LoadAsync()
    {
        Session = SessionId.HasValue
            ? await _db.GameSessions.FirstOrDefaultAsync(x => x.Id == SessionId.Value)
            : await _db.GameSessions.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

        if (Session is null) return;

        Quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.Id == Session.QuizId);
        Players = await _db.Players.Where(x => x.GameSessionId == Session.Id).OrderBy(x => x.Username).ToListAsync();
        CurrentQuestion = await _gameService.GetCurrentQuestionAsync(Session.Id);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizGame.Data;
using QuizGame.Models;
using QuizGame.Services;

namespace QuizGame.Pages;

public class PlayModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly GameService _gameService;

    public PlayModel(AppDbContext db, GameService gameService)
    {
        _db = db;
        _gameService = gameService;
    }

    [BindProperty(SupportsGet = true)] public int SessionId { get; set; }
    [BindProperty(SupportsGet = true)] public int PlayerId { get; set; }
    [BindProperty] public string SelectedAnswer { get; set; } = string.Empty;

    public GameSession? Session { get; set; }
    public Player? Player { get; set; }
    public Question? Question { get; set; }
    public bool AlreadyAnswered { get; set; }
    public int SecondsLeft { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (Session is null || Player is null)
            return RedirectToPage("/Join");

        if (!Player.IsAdmitted)
            return RedirectToPage("/Lobby", new { sessionId = SessionId, playerId = PlayerId });

        if (Session?.Status == SessionStatus.Finished)
            return RedirectToPage("/Results", new { sessionId = SessionId, playerId = PlayerId });
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();
        if (Session is null || Player is null || Question is null)
            return RedirectToPage("/Join");

        if (Session.Status == SessionStatus.QuestionLive && !AlreadyAnswered)
            await _gameService.SubmitAnswerAsync(SessionId, PlayerId, SelectedAnswer);

        return RedirectToPage(new { sessionId = SessionId, playerId = PlayerId });
    }

    private async Task LoadAsync()
    {
        Session = await _db.GameSessions.Include(s => s.Quiz).FirstOrDefaultAsync(s => s.Id == SessionId);
        Player = await _db.Players.FirstOrDefaultAsync(p => p.Id == PlayerId && p.GameSessionId == SessionId);
        if (Session is null || Player is null) return;

        Question = await _gameService.GetCurrentQuestionAsync(Session.Id);
        if (Question is not null)
            AlreadyAnswered = await _db.PlayerAnswers.AnyAsync(a => a.PlayerId == Player.Id && a.QuestionId == Question.Id);

        if (Session.QuestionEndsAtUtc.HasValue)
            SecondsLeft = Math.Max(0, (int)Math.Ceiling((Session.QuestionEndsAtUtc.Value - DateTime.UtcNow).TotalSeconds));
    }
}

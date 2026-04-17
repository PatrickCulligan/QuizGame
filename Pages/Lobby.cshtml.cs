using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizGame.Data;
using QuizGame.Models;

namespace QuizGame.Pages;

public class LobbyModel : PageModel
{
    private readonly AppDbContext _db;
    public LobbyModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)] public int SessionId { get; set; }
    [BindProperty(SupportsGet = true)] public int PlayerId { get; set; }
    public GameSession? Session { get; set; }
    public Player? Player { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Session = await _db.GameSessions.FirstOrDefaultAsync(x => x.Id == SessionId);
        Player = await _db.Players.FirstOrDefaultAsync(x => x.Id == PlayerId && x.GameSessionId == SessionId);

        if (Session is null || Player is null)
            return RedirectToPage("/Join");

        if (Session?.Status == SessionStatus.QuestionLive || Session?.Status == SessionStatus.QuestionClosed)
        {
            if (!Player.IsAdmitted)
                return Page();

            return RedirectToPage("/Play", new { sessionId = SessionId, playerId = PlayerId });
        }

        if (Session?.Status == SessionStatus.Finished)
            return RedirectToPage("/Results", new { sessionId = SessionId, playerId = PlayerId });

        return Page();
    }
}

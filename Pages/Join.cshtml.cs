using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using QuizGame.Hubs;
using QuizGame.Services;

namespace QuizGame.Pages;

public class JoinModel : PageModel
{
    private readonly GameService _gameService;
    private readonly IHubContext<GameHub> _hub;

    public JoinModel(GameService gameService, IHubContext<GameHub> hub)
    {
        _gameService = gameService;
        _hub = hub;
    }

    [BindProperty, Required]
    public string Pin { get; set; } = string.Empty;

    [BindProperty, Required, StringLength(50)]
    public string Username { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await _gameService.JoinAsync(Pin.Trim(), Username.Trim());
        if (!result.ok || result.player is null || result.session is null)
        {
            Message = result.message;
            return Page();
        }

        await _hub.Clients.Group($"session-{result.session.Id}").SendAsync("PlayersUpdated");
        return RedirectToPage("/Lobby", new { sessionId = result.session.Id, playerId = result.player.Id });
    }
}

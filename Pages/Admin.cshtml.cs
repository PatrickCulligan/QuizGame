using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizGame.Data;
using QuizGame.Models;
using QuizGame.Services;

namespace QuizGame.Pages;

public class AdminModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly OpenAiQuizService _openAiQuizService;
    private readonly GameService _gameService;

    public AdminModel(AppDbContext db, OpenAiQuizService openAiQuizService, GameService gameService)
    {
        _db = db;
        _openAiQuizService = openAiQuizService;
        _gameService = gameService;
    }

    [BindProperty]
    public AdminInput Input { get; set; } = new();

    public Quiz? CurrentQuiz { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SessionPin { get; set; }
    public int? SessionId { get; set; }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }

        _db.PlayerAnswers.RemoveRange(_db.PlayerAnswers);
        _db.Players.RemoveRange(_db.Players);
        _db.GameSessions.RemoveRange(_db.GameSessions);
        _db.Questions.RemoveRange(_db.Questions);
        _db.Quizzes.RemoveRange(_db.Quizzes);
        await _db.SaveChangesAsync();

        var generatedQuestions = await _openAiQuizService.GenerateQuestionsAsync(Input.QuestionCount, Input.Topic, Input.Difficulty);
        var quiz = new Quiz
        {
            Title = Input.Title,
            Topic = Input.Topic,
            Difficulty = Input.Difficulty,
            QuestionCount = Input.QuestionCount,
            SecondsPerQuestion = Input.SecondsPerQuestion,
            CreatedAtUtc = DateTime.UtcNow,
            Questions = generatedQuestions
        };

        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync();

        Message = "Quiz generated successfully.";
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateSessionAsync()
    {
        var quiz = await _db.Quizzes.OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        if (quiz is null)
        {
            Message = "Create a quiz first.";
            await LoadAsync();
            return Page();
        }

        var session = await _gameService.CreateSessionAsync(quiz.Id);
        SessionPin = session.Pin;
        SessionId = session.Id;
        await LoadAsync();
        Message = "Live game session created.";
        return Page();
    }

    private async Task LoadAsync()
    {
        CurrentQuiz = await _db.Quizzes.OrderByDescending(q => q.Id).FirstOrDefaultAsync();
        if (CurrentQuiz is not null)
        {
            Input = new AdminInput
            {
                Title = CurrentQuiz.Title,
                Topic = CurrentQuiz.Topic,
                Difficulty = CurrentQuiz.Difficulty,
                QuestionCount = CurrentQuiz.QuestionCount,
                SecondsPerQuestion = CurrentQuiz.SecondsPerQuestion
            };
        }
    }

    public class AdminInput
    {
        [Required] public string Title { get; set; } = "Live Quiz";
        [Required] public string Topic { get; set; } = "General Knowledge";
        [Required] public string Difficulty { get; set; } = "Easy";
        [Range(1, 20)] public int QuestionCount { get; set; } = 5;
        [Range(5, 60)] public int SecondsPerQuestion { get; set; } = 20;
    }
}

using System.ComponentModel.DataAnnotations;

namespace QuizGame.Models;

public class Quiz
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Topic { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Difficulty { get; set; } = "Easy";

    [Range(1, 50)]
    public int QuestionCount { get; set; } = 5;

    [Range(5, 120)]
    public int SecondsPerQuestion { get; set; } = 20;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<Question> Questions { get; set; } = new();
}

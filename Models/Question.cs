using System.ComponentModel.DataAnnotations;

namespace QuizGame.Models;

public class Question
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = default!;
    public int OrderIndex { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;
    [Required]
    public string OptionA { get; set; } = string.Empty;
    [Required]
    public string OptionB { get; set; } = string.Empty;
    [Required]
    public string OptionC { get; set; } = string.Empty;
    [Required]
    public string OptionD { get; set; } = string.Empty;
    [Required]
    public string CorrectAnswer { get; set; } = string.Empty;
}

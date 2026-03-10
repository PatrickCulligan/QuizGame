using QuizGame.Models;

namespace QuizGame.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (db.Quizzes.Any()) return;

        db.Quizzes.Add(new Quiz
        {
            Title = "Demo Quiz",
            Topic = "General Knowledge",
            Difficulty = "Easy",
            QuestionCount = 3,
            SecondsPerQuestion = 15,
            CreatedAtUtc = DateTime.UtcNow,
            Questions = new List<Question>
            {
                new() { OrderIndex = 1, Text = "What color is the sky on a clear day?", OptionA = "Blue", OptionB = "Red", OptionC = "Green", OptionD = "Yellow", CorrectAnswer = "Blue" },
                new() { OrderIndex = 2, Text = "How many days are in a week?", OptionA = "5", OptionB = "6", OptionC = "7", OptionD = "8", CorrectAnswer = "7" },
                new() { OrderIndex = 3, Text = "Which animal says meow?", OptionA = "Dog", OptionB = "Cat", OptionC = "Cow", OptionD = "Bird", CorrectAnswer = "Cat" }
            }
        });

        await db.SaveChangesAsync();
    }
}

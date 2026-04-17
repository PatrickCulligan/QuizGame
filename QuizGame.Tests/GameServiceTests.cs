using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuizGame.Data;
using QuizGame.Models;
using QuizGame.Services;
using Xunit;

namespace QuizGame.Tests;

public class GameServiceTests
{
    [Fact]
    public async Task JoinAsync_creates_new_players_as_not_admitted()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var service = new GameService(fixture.DbContext);
        var session = await fixture.CreateSessionAsync();

        var result = await service.JoinAsync(session.Pin, "alice");

        Assert.True(result.ok);
        Assert.NotNull(result.player);
        Assert.False(result.player!.IsAdmitted);
    }

    [Fact]
    public async Task SubmitAnswerAsync_ignores_players_from_another_session()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var service = new GameService(fixture.DbContext);
        var session = await fixture.CreateLiveSessionAsync();
        var otherSession = await fixture.CreateSessionAsync();
        var player = await fixture.CreatePlayerAsync(otherSession.Id, "intruder", admitted: true);

        await service.SubmitAnswerAsync(session.Id, player.Id, "Blue");

        Assert.Equal(0, await fixture.DbContext.PlayerAnswers.CountAsync());
    }

    [Fact]
    public async Task SubmitAnswerAsync_rejects_unadmitted_players()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var service = new GameService(fixture.DbContext);
        var session = await fixture.CreateLiveSessionAsync();
        var player = await fixture.CreatePlayerAsync(session.Id, "waiting-player", admitted: false);

        await service.SubmitAnswerAsync(session.Id, player.Id, "Blue");

        Assert.Equal(0, await fixture.DbContext.PlayerAnswers.CountAsync());
    }

    [Fact]
    public async Task SubmitAnswerAsync_rejects_answers_after_question_end()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var service = new GameService(fixture.DbContext);
        var session = await fixture.CreateLiveSessionAsync(questionEndedSecondsAgo: 5);
        var player = await fixture.CreatePlayerAsync(session.Id, "late-player", admitted: true);

        await service.SubmitAnswerAsync(session.Id, player.Id, "Blue");

        Assert.Equal(0, await fixture.DbContext.PlayerAnswers.CountAsync());
    }

    [Fact]
    public async Task SubmitAnswerAsync_records_points_for_a_valid_answer()
    {
        await using var fixture = await TestDbFixture.CreateAsync();
        var service = new GameService(fixture.DbContext);
        var session = await fixture.CreateLiveSessionAsync();
        var player = await fixture.CreatePlayerAsync(session.Id, "fast-player", admitted: true);

        await service.SubmitAnswerAsync(session.Id, player.Id, "Blue");

        var answer = await fixture.DbContext.PlayerAnswers.SingleAsync();
        Assert.True(answer.IsCorrect);
        Assert.True(answer.AwardedPoints >= 500);
    }

    private sealed class TestDbFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestDbFixture(SqliteConnection connection, AppDbContext dbContext)
        {
            _connection = connection;
            DbContext = dbContext;
        }

        public AppDbContext DbContext { get; }

        public static async Task<TestDbFixture> CreateAsync()
        {
            // Keep one in-memory SQLite connection open so EF uses a relational database with indexes.
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var dbContext = new AppDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new TestDbFixture(connection, dbContext);
        }

        public async Task<GameSession> CreateSessionAsync()
        {
            var quiz = await CreateQuizAsync();
            var session = new GameSession
            {
                QuizId = quiz.Id,
                Quiz = quiz,
                Pin = Guid.NewGuid().ToString("N")[..6],
                Status = SessionStatus.Lobby
            };

            DbContext.GameSessions.Add(session);
            await DbContext.SaveChangesAsync();
            return session;
        }

        public async Task<GameSession> CreateLiveSessionAsync(int questionEndedSecondsAgo = -1)
        {
            var quiz = await CreateQuizAsync();
            var now = DateTime.UtcNow;
            var session = new GameSession
            {
                QuizId = quiz.Id,
                Quiz = quiz,
                Pin = Guid.NewGuid().ToString("N")[..6],
                Status = SessionStatus.QuestionLive,
                CurrentQuestionIndex = 1,
                QuestionStartedAtUtc = now.AddSeconds(-2),
                QuestionEndsAtUtc = questionEndedSecondsAgo >= 0
                    ? now.AddSeconds(-questionEndedSecondsAgo)
                    : now.AddSeconds(quiz.SecondsPerQuestion - 2)
            };

            DbContext.GameSessions.Add(session);
            await DbContext.SaveChangesAsync();
            return session;
        }

        public async Task<Player> CreatePlayerAsync(int sessionId, string username, bool admitted)
        {
            // Explicit test player creation keeps admission state obvious in each test body.
            var player = new Player
            {
                GameSessionId = sessionId,
                Username = username,
                IsAdmitted = admitted
            };

            DbContext.Players.Add(player);
            await DbContext.SaveChangesAsync();
            return player;
        }

        private async Task<Quiz> CreateQuizAsync()
        {
            var existingQuiz = await DbContext.Quizzes
                .Include(q => q.Questions)
                .OrderByDescending(q => q.Id)
                .FirstOrDefaultAsync();

            if (existingQuiz is not null)
                return existingQuiz;

            var quiz = new Quiz
            {
                Title = "Test Quiz",
                Topic = "Testing",
                Difficulty = "Easy",
                QuestionCount = 1,
                SecondsPerQuestion = 20,
                Questions = new List<Question>
                {
                    new()
                    {
                        OrderIndex = 1,
                        Text = "What color is the sky?",
                        OptionA = "Blue",
                        OptionB = "Red",
                        OptionC = "Green",
                        OptionD = "Yellow",
                        CorrectAnswer = "Blue"
                    }
                }
            };

            DbContext.Quizzes.Add(quiz);
            await DbContext.SaveChangesAsync();
            return quiz;
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}

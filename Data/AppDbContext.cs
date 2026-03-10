using Microsoft.EntityFrameworkCore;
using QuizGame.Models;

namespace QuizGame.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerAnswer> PlayerAnswers => Set<PlayerAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quiz>()
            .HasMany(q => q.Questions)
            .WithOne(q => q.Quiz)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameSession>().HasIndex(x => x.Pin).IsUnique();
        modelBuilder.Entity<Player>().HasIndex(x => new { x.GameSessionId, x.Username }).IsUnique();
        modelBuilder.Entity<PlayerAnswer>().HasIndex(x => new { x.PlayerId, x.QuestionId }).IsUnique();
    }
}

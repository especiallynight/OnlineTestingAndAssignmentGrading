
using ExamService.Models;
using Microsoft.EntityFrameworkCore;
using MassTransit;

namespace ExamService.Data
{
    public class ExamDbContext : DbContext
    {
        public ExamDbContext(DbContextOptions<ExamDbContext> options)
            : base(options)
        { }

        public DbSet<Answer> Answers { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            modelBuilder.Entity<ExamAttempt>()
                .HasMany(attempt => attempt.Answers)
                .WithOne(answer => answer.Attempt)
                .HasForeignKey(answer => answer.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Answer>()
                .HasIndex(answer => new { answer.AttemptId, answer.QuestionId })
                .IsUnique();

            modelBuilder.Entity<Answer>()
                .Property(answer => answer.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            modelBuilder.Entity<ExamAttempt>()
                .Property(attempt => attempt.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            modelBuilder.Entity<ExamAttempt>()
                .HasKey(attempt => attempt.AttemptId);
        }
    }
}

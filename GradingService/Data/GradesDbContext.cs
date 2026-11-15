
using GradingService.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace GradingService.Data
{
    public class GradesDbContext : DbContext
    {
        public GradesDbContext(DbContextOptions<GradesDbContext> options)
            : base(options) { }

        public DbSet<AttemptGrade> AttemptGrades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Встроенный Outbox MassTransit: OutboxMessage + OutboxState,
            // а InboxState защищает от повторной обработки сообщений.
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            modelBuilder.Entity<AttemptGrade>()
                .HasIndex(grade => grade.AttemptId)
                .IsUnique();
        }
    }
}

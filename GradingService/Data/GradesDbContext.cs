
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
        public DbSet<OutboxGrade> OutboxGrades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            modelBuilder.Entity<OutboxGrade>()
                .HasIndex(message => message.PublishedAt);

            modelBuilder.Entity<AttemptGrade>()
                .HasIndex(grade => grade.AttemptId)
                .IsUnique();

            modelBuilder.Entity<OutboxGrade>()
                .Property(message => message.Payload)
                .HasColumnType("jsonb");

            modelBuilder.Entity<OutboxGrade>()
                .HasKey(message => message.OutboxMessageId);
        }
    }
}

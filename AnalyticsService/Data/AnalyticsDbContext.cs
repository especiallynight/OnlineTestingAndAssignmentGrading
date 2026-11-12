using AnalyticsService.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Data
{
    public class AnalyticsDbContext : DbContext
    {
        public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
            : base(options)
        {
        }
        public DbSet<StudentStatistics> StudentStatistics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StudentStatistics>()
                .HasIndex(student => new { student.StudentId, student.CourseId })
                .IsUnique();
            
            modelBuilder.Entity<StudentStatistics>()
                .HasKey(statistics => statistics.StudentStatisticId);

            modelBuilder.Entity<StudentStatistics>()
                .Property(s => s.ProcessedAttemptIds)
                .HasColumnType("integer[]");
            
        }

    }
}

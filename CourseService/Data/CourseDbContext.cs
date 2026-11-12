using CourseService.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseService.Data
{
    public class CourseDbContext:DbContext
    {
        public CourseDbContext (DbContextOptions<CourseDbContext> options)
            :base (options)
            { }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Course>()
                .HasMany(course => course.Assignments)
                .WithOne(assignment => assignment.Course)
                .HasForeignKey(assignment => assignment.CourseId);

            modelBuilder.Entity<Assignment>()
                .HasMany(assignment => assignment.Questions)
                .WithOne(question => question.Assignment)
                .HasForeignKey(question => question.AssignmentId);
            
            modelBuilder.Entity<Question>()
                .Property(question => question.QuestionOptions)
                .HasColumnType("jsonb");

            modelBuilder.Entity<Enrollment>()
                .HasOne(enrollment => enrollment.Course)
                .WithMany()
                .HasForeignKey(enrollment => enrollment.CourseId);

            modelBuilder.Entity<Enrollment>()
                .HasIndex(enrollment => new { enrollment.StudentId, enrollment.CourseId })
                .IsUnique();
            
            modelBuilder.Entity<Question>()
                .Property(question => question.TestCases)
                .HasColumnType("jsonb");
            
            modelBuilder.Entity<Question>()
                .Property(question => question.CorrectAnswer)
                .HasColumnType("jsonb");


        }
    }
}

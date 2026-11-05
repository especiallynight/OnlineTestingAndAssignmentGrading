using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Data
{
    public class IdentityDbContext: DbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext>options)
            :base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<TeacherApplication> TeacherApplications { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TeacherApplication>()
                .HasOne(application => application.User)
                .WithMany(user => user.TeacherApplications)
                .HasForeignKey(application => application.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeacherApplication>()
                .HasOne(application => application.Reviewer)
                .WithMany(user => user.ReviewedApplications) 
                .HasForeignKey(application => application.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId);
            
            modelBuilder.Entity<User>()
                .HasIndex(user => user.Email)
                .IsUnique();
            
            modelBuilder.Entity<RefreshToken>()
                .HasIndex(token => token.TokenHash)
                .IsUnique();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Models.EF
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<SchoolClass> Classes { get; set; }
        public DbSet<StudentClass> StudentClasses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Score> Scores { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<VocabularyTopic> VocabularyTopics { get; set; }
        public DbSet<ClassExercise> ClassExercises { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Submission>(entity =>
            {
                entity.HasKey(s => s.SubmissionId);

                entity.HasOne(s => s.Exercise)
                      .WithMany(e => e.Submissions)
                      .HasForeignKey(s => s.ExerciseId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Student)
                      .WithMany(u => u.Submissions)
                      .HasForeignKey(s => s.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StudentClass>()
                .HasOne(sc => sc.Student)
                .WithMany(u => u.StudentClasses)
                .HasForeignKey(sc => sc.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentClass>()
                .HasOne(sc => sc.SchoolClass)
                .WithMany(c => c.StudentClasses)
                .HasForeignKey(sc => sc.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassExercise>()
                .HasOne(ce => ce.SchoolClass)
                .WithMany(c => c.ClassExercises)
                .HasForeignKey(ce => ce.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassExercise>()
                .HasOne(ce => ce.Exercise)
                .WithMany(e => e.ClassExercises)
                .HasForeignKey(ce => ce.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .Property(n => n.IsRead)
                .HasDefaultValue(false);
        }
    }
}
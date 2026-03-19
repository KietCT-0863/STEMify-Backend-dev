using Classroom.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Classroom.Infrastructure.Persistence
{
    public class ClassroomDbContext : DbContext
    {
        public ClassroomDbContext(DbContextOptions<ClassroomDbContext> options)
            : base(options) { }

        // DbSet properties for your entities go here
        public DbSet<Domain.Entities.Classroom> Classrooms { get; set; }
        public DbSet<Annoucement> Annoucements { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        public DbSet<StudentSectionProgress> StudentSectionProgress { get; set; }
        public DbSet<StudentLessonProgress> StudentLessonProgress { get; set; }
        public DbSet<CurriculumEnrollment> CurriculumEnrollments { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<StudentQuiz> StudentQuizzes { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<QuizQuestionAttempt> QuizQuestionAttempts { get; set; }
        public DbSet<AnswerAttempt> AnswerAttempts { get; set; }
        public DbSet<ClassroomStudent> ClassroomStudents { get; set; }
        public DbSet<StudentAssignment> StudentAssignments { get; set; }
        public DbSet<RubricScore> RubricScores { get; set; }
        public DbSet<AssignmentQuestionAttempt> AssignmentQuestionAttempts { get; set; }
        public DbSet<AssignmentAttempt> AssignmentAttempts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
        }
    }
}

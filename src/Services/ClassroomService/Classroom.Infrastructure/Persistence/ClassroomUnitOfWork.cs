using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Repositories;
using Contracts.Abstractions.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Infrastructure.Persistence
{
    public class ClassroomUnitOfWork : IEfUnitOfWork<ClassroomDbContext>, IClassroomUnitOfWork
    {
        private readonly ClassroomDbContext _context;

        public ClassroomDbContext DbContext => _context;

        /// <summary>
        /// Annoucement repository - injected via DI
        /// </summary>
        public IAnnoucementRepository Annoucements { get; }

        /// <summary>
        /// Classroom repository - injected via DI
        /// </summary>
        public IClassroomRepository Classrooms { get; }

        /// <summary>
        /// CourseEnrollment repository - injected via DI
        /// </summary>
        public ICourseEnrollmentRepository CourseEnrollments { get; }

        /// <summary>
        /// Lesson progress repository - injected via DI
        /// </summary>
        public ILessonProgressRepository LessonProgress { get; }

        /// <summary>
        /// Classroom resource repository - injected via DI
        /// </summary>
        public ISectionProgressRepository SectionProgress { get; }

        public ICurriculumEnrollmentRepository CurriculumEnrollments { get; }
        public ICertificateRepository Certificates { get; }
        public IStudentQuizRepository StudentQuizzes { get; }
        public IQuizAttemptRepository QuizAttempts { get; }
        public IClassroomStudentRepository ClassroomStudents { get; }
        public IRubricScoreRepository RubricScores { get; }
        public IAssignmentQuestionAttemptRepository AssignmentQuestionAttempts { get; }
        public IAssignmentAttemptRepository AssignmentAttempts { get; }
        public IStudentAssignmentRepository StudentAssignments { get; }

        /// <summary>
        /// Constructor with dependency injection for all repositories
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="annoucementRepository">Annoucement repository</param>
        /// <param name="classroomRepository">Classroom repository</param>
        public ClassroomUnitOfWork(
            ClassroomDbContext context,
            IAnnoucementRepository annoucementRepository,
            IClassroomRepository classroomRepository,
            ICourseEnrollmentRepository enrollmentRepository,
            ILessonProgressRepository lessonProgressRepository,
            ICurriculumEnrollmentRepository curriculumEnrollmentRepository,
            ICertificateRepository certificateRepository,
            ISectionProgressRepository sectionProgressRepository,
            IStudentQuizRepository studentQuizRepository,
            IQuizAttemptRepository quizAttemptRepository,
            IRubricScoreRepository rubricScoreRepository,
            IAssignmentAttemptRepository assignmentAttemptRepository,
            IAssignmentQuestionAttemptRepository assignmentQuestionAttemptRepository,
            IStudentAssignmentRepository studentAssignmentRepository,
            IClassroomStudentRepository classroomStudentRepository
        )
        {
            _context = context;
            Annoucements =
                annoucementRepository
                ?? throw new ArgumentNullException(nameof(annoucementRepository));
            Classrooms =
                classroomRepository ?? throw new ArgumentNullException(nameof(classroomRepository));
            CourseEnrollments =
                enrollmentRepository
                ?? throw new ArgumentNullException(nameof(enrollmentRepository));
            CourseEnrollments =
                enrollmentRepository
                ?? throw new ArgumentNullException(nameof(enrollmentRepository));
            LessonProgress =
                lessonProgressRepository
                ?? throw new ArgumentNullException(nameof(lessonProgressRepository));
            SectionProgress =
                sectionProgressRepository
                ?? throw new ArgumentNullException(nameof(sectionProgressRepository));
            CurriculumEnrollments =
                curriculumEnrollmentRepository
                ?? throw new ArgumentNullException(nameof(curriculumEnrollmentRepository));
            Certificates =
                certificateRepository
                ?? throw new ArgumentNullException(nameof(certificateRepository));
            RubricScores =
                rubricScoreRepository
                ?? throw new ArgumentNullException(nameof(rubricScoreRepository));
            AssignmentAttempts =
                assignmentAttemptRepository
                ?? throw new ArgumentNullException(nameof(assignmentAttemptRepository));
            AssignmentQuestionAttempts =
                assignmentQuestionAttemptRepository
                ?? throw new ArgumentNullException(nameof(assignmentQuestionAttemptRepository));
            StudentAssignments =
                studentAssignmentRepository
                ?? throw new ArgumentNullException(nameof(studentAssignmentRepository));
            StudentQuizzes = studentQuizRepository;
            QuizAttempts = quizAttemptRepository;
            ClassroomStudents = classroomStudentRepository;
        }

        public DbSet<TEntity> Set<TEntity>()
            where TEntity : class
        {
            return _context.Set<TEntity>();
        }

        public Task BeginTransactionAsync(
            System.Data.IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default
        )
        {
            return _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.CommitTransactionAsync(cancellationToken);
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.RollbackTransactionAsync(cancellationToken);
            }
        }

        public Task RetryOnExceptionAsync(Func<Task> operation)
        {
            // Simple implementation - just execute the operation
            return operation();
        }

        public Task<TResult> RetryOnExceptionAsync<TResult>(Func<Task<TResult>> operation)
        {
            // Simple implementation - just execute the operation
            return operation();
        }

        public async Task ExecuteTransactionalAsync(
            Func<Task> action,
            CancellationToken cancellationToken = default
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync(
                cancellationToken
            );
            try
            {
                await action();
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<T> ExecuteTransactionalAsync<T>(
            Func<Task<T>> action,
            CancellationToken cancellationToken = default
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync(
                cancellationToken
            );
            try
            {
                var result = await action();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}

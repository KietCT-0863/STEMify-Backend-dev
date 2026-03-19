using Contracts.Abstractions.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Resource.Application.Common.Interfaces;
using Resource.Application.Common.Interfaces.Repositories;
using Resource.Infrastructure.Persistence;

namespace Resource.Infrastructure.Data
{
    public class ResourceUnitOfWork : IEfUnitOfWork<ResourceDbContext>, IResourceUnitOfWork
    {
        private readonly ResourceDbContext _context;

        public ResourceDbContext DbContext => _context;

        /// <summary>
        /// Category repository - injected via DI
        /// </summary>
        public ITopicRepository Topics { get; }

        /// <summary>
        /// Age range repository - injected via DI
        /// </summary>
        public IAgeRangeRepository AgeRanges { get; }

        /// <summary>
        /// Course repository - injected via DI
        /// </summary>
        public ICourseRepository Courses { get; }

        /// <summary>
        /// Skill repository - injected via DI
        /// </summary>
        public ISkillRepository Skills { get; }

        /// <summary>
        /// Standard repository - injected via DI
        /// </summary>
        public IStandardRepository Standards { get; }

        /// <summary>
        /// Lesson repository - injected via DI
        /// </summary>
        public ILessonRepository Lessons { get; }

        /// <summary>
        /// Section repository - injected via DI
        /// </summary>
        public ISectionRepository Sections { get; }

        /// <summary>
        /// Content repository - injected via DI
        /// </summary>
        public IContentRepository Contents { get; }

        /// <summary>
        /// Quiz repository - injected via DI
        /// </summary>
        public IQuizRepository Quizzes { get; }

        /// <summary>
        /// Question repository - injected via DI
        /// </summary>
        public IQuestionRepository Questions { get; }

        /// <summary>
        /// Answer repository - injected via DI
        /// </summary>
        public IAnswerRepository Answers { get; }

        /// <summary>
        /// Curriculum repository - injected via DI
        /// </summary>
        public ICurriculumRepository Curriculums { get; }

        /// <summary>
        /// Program Learning Outcome repository - injected via DI
        /// </summary>
        public IProgramLearningOutcomeRepository ProgramLearningOutcomes { get; }

        /// <summary>
        /// Course Learning Outcome repository - injected via DI
        /// </summary>
        public ICourseLearningOutcomeRepository CourseLearningOutcomes { get; }
        /// <summary>
        /// Curriculum Course repository - injected via DI
        /// </summary>
        public ICurriculumCourseRepository CurriculumCourses { get; }
        public ILessonAssetRepository LessonAssets { get; }
        public ILessonAssetTagRepository LessonAssetTags { get; }
        public ITagRepository Tags { get; }
        public IAssignmentQuestionRepository AssignmentQuestions { get; }
        public IAssignmentRepository Assignments { get; }
        public IRubricCriterionRepository RubricCriterions { get; }
        public ICurriculumEmulationRepository CurriculumEmulations { get; }

        /// <summary>
        /// Constructor with dependency injection for all repositories
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="categoryRepository">Category repository</param>
        /// <param name="ageRangeRepository">Age range repository</param>
        /// <param name="courseRepository">Course repository</param>
        /// <param name="skillRepository">Skill repository</param>
        /// <param name="standardRepository">Standard repository</param>
        /// <param name="lessonRepository">lesson repository</param>
        /// <param name="sectionRepository">Section repository</param>
        /// <param name="contentRepository">Content repository</param>
        /// <param name="quizRepository">Quiz repository</param>
        /// <param name="questionRepository">Question repository</param>
        /// <param name="answerRepository">Answer repository</param>
        /// <param name="curriculumRepository">Curriculum repository</param>
        /// <param name="programLearningOutcomeRepository">Program Learning Outcome repository</param>
        /// <param name="courseLearningOutcomeRepository">Course Learning Outcome repository</param>
        /// <param name="curriculumCourseRepository">Curriculum course repository</param>
        public ResourceUnitOfWork(
            ResourceDbContext context,
            ITopicRepository topicRepository,
            IAgeRangeRepository ageRangeRepository,
            ICourseRepository courseRepository,
            ISkillRepository skillRepository,
            IStandardRepository standardRepository,
            ILessonRepository lessonRepository,
            ISectionRepository sectionRepository,
            IContentRepository contentRepository,
            IQuizRepository quizRepository,
            IQuestionRepository questionRepository,
            IAnswerRepository answerRepository,
            ICurriculumRepository curriculumRepository,
            IProgramLearningOutcomeRepository programLearningOutcomeRepository,
            ICourseLearningOutcomeRepository courseLearningOutcomeRepository,
            ICurriculumCourseRepository curriculumCourseRepository,
            ILessonAssetRepository lessonAssetRepository,
            ILessonAssetTagRepository lessonAssetTagRepository,
            IAssignmentRepository assignmentRepository,
            IRubricCriterionRepository rubricCriterionRepository,
            IAssignmentQuestionRepository assignmentQuestionRepository,
            ICurriculumEmulationRepository curriculumEmulationRepository,
            ITagRepository tagRepository
        )
        {
            _context = context;
            Topics = topicRepository;
            AgeRanges = ageRangeRepository;
            Courses = courseRepository;
            Skills = skillRepository;
            Standards = standardRepository;
            Lessons = lessonRepository;
            Sections = sectionRepository;
            Contents = contentRepository;
            Quizzes = quizRepository;
            Questions = questionRepository;
            Answers = answerRepository;
            Curriculums = curriculumRepository;
            ProgramLearningOutcomes = programLearningOutcomeRepository;
            CourseLearningOutcomes = courseLearningOutcomeRepository;
            CurriculumCourses = curriculumCourseRepository;
            LessonAssets = lessonAssetRepository;
            LessonAssetTags = lessonAssetTagRepository;
            Tags = tagRepository;
            Assignments = assignmentRepository;
            AssignmentQuestions = assignmentQuestionRepository;
            RubricCriterions = rubricCriterionRepository;
            CurriculumEmulations = curriculumEmulationRepository;
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

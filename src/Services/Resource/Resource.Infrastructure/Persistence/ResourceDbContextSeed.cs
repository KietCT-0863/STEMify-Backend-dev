using Contracts.Abstractions.Services;
using Infrastructure.Common;
using Resource.Domain.Constants;
using Resource.Domain.Entities;

namespace Resource.Infrastructure.Persistence
{
    public class ResourceDbContextSeed
    {
        private readonly ResourceDbContext _dbContext;
        private readonly IFileReader _fileReader;

        public ResourceDbContextSeed(ResourceDbContext dbContext, IFileReader fileReader)
        {
            _dbContext = dbContext;
            _fileReader = fileReader;
        }

        public async Task SeedAsync()
        {
            var rootPath = AppCts.AbsoluteProjectPath;

            await new JsonDataSeeder<AgeRange, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.AgeRangePath)
                .SeedAsync();

            await new JsonDataSeeder<Course, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.CoursePath)
                .SeedAsync();

            await new JsonDataSeeder<Topic, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.TopicPath)
                .SeedAsync();

            await new JsonDataSeeder<Skill, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.SkillPath)
                .SeedAsync();

            await new JsonDataSeeder<Standard, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.StandardPath)
                .SeedAsync();
            await new JsonDataSeeder<Lesson, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.LessonPath)
                .SeedAsync();

            await new JsonDataSeeder<LessonTopic, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.LessonTopicPath)
                .SeedAsync();

            await new JsonDataSeeder<LessonSkill, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.LessonSkillPath)
                .SeedAsync();

            await new JsonDataSeeder<LessonStandard, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.LessonStandardPath)
                .SeedAsync();

            await new JsonDataSeeder<Section, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.SectionPath)
                .SeedAsync();

            await new JsonDataSeeder<Content, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.ContentPath)
                .SeedAsync();

            await new JsonDataSeeder<Curriculum, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.CurriculumPath)
                .SeedAsync();

            await new JsonDataSeeder<ProgramLearningOutcome, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.ProgramLearningOutcomePath)
                .SeedAsync();

            await new JsonDataSeeder<CourseLearningOutcome, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.CourseLearningOutcomePath)
                .SeedAsync();

            await new JsonDataSeeder<LearningOutcomeMapping, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.LearningOutcomeMappingPath)
                .SeedAsync();

            await new JsonDataSeeder<CurriculumCourse, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.CurriculumCoursePath)
                .SeedAsync();

            await new JsonDataSeeder<Quiz, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.QuizPath)
                .SeedAsync();
            await new JsonDataSeeder<Question, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.QuestionPath)
                .SeedAsync();
            await new JsonDataSeeder<Answer, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.AnswerPath)
                .SeedAsync();

            await new JsonDataSeeder<Assignment, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.AssignmentPath)
                .SeedAsync();

            await new JsonDataSeeder<AssignmentQuestion, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.AssignmentQuestionPath)
                .SeedAsync();

            await new JsonDataSeeder<RubricCriterion, ResourceDbContext>(_fileReader, _dbContext)
                .AddRelativeFilePath(rootPath, AppCts.SeederRelativePath.RubricCriterionPath)
                .SeedAsync();
        }
    }
}

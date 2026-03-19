using Contracts.Abstractions.Persistence.EfCore;
using Resource.Application.Common.Interfaces.Repositories;

namespace Resource.Application.Common.Interfaces
{
    public interface IResourceUnitOfWork : IEfUnitOfWork
    {
        ITopicRepository Topics { get; }
        IAgeRangeRepository AgeRanges { get; }
        ICourseRepository Courses { get; }
        ISkillRepository Skills { get; }
        IStandardRepository Standards { get; }
        IAnswerRepository Answers { get; }
        ILessonRepository Lessons { get; }
        ISectionRepository Sections { get; }
        IContentRepository Contents { get; }
        IQuizRepository Quizzes { get; }
        IQuestionRepository Questions { get; }
        ICurriculumRepository Curriculums { get; }
        ICourseLearningOutcomeRepository CourseLearningOutcomes { get; }
        IProgramLearningOutcomeRepository ProgramLearningOutcomes { get; }
        ICurriculumCourseRepository CurriculumCourses { get; }
        ILessonAssetRepository LessonAssets { get; }
        ILessonAssetTagRepository LessonAssetTags { get; }
        ITagRepository Tags { get; }
        IAssignmentQuestionRepository AssignmentQuestions { get; }
        IAssignmentRepository Assignments { get; }
        IRubricCriterionRepository RubricCriterions { get; }
        ICurriculumEmulationRepository CurriculumEmulations { get; }
    }
}

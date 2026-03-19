using Classroom.Application.Common.Interfaces.Repositories;
using Contracts.Abstractions.Persistence.EfCore;

namespace Classroom.Application.Common.Interfaces
{
    public interface IClassroomUnitOfWork : IEfUnitOfWork
    {
        IAnnoucementRepository Annoucements { get; }
        IClassroomRepository Classrooms { get; }
        ICourseEnrollmentRepository CourseEnrollments { get; }
        ILessonProgressRepository LessonProgress { get; }
        ISectionProgressRepository SectionProgress { get; }
        ICurriculumEnrollmentRepository CurriculumEnrollments { get; }
        ICertificateRepository Certificates { get; }
        IStudentQuizRepository StudentQuizzes { get; }
        IQuizAttemptRepository QuizAttempts { get; }
        IClassroomStudentRepository ClassroomStudents { get; }
        IRubricScoreRepository RubricScores { get; }
        IAssignmentQuestionAttemptRepository AssignmentQuestionAttempts { get; }
        IAssignmentAttemptRepository AssignmentAttempts { get; }
        IStudentAssignmentRepository StudentAssignments { get; }
    }
}

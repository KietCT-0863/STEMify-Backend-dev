using System.Reflection;

namespace Resource.Domain.Constants
{
    public class AppCts
    {
        public static readonly string AbsoluteProjectPath =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

        /// <summary>
        /// Location of Fake Json Filepath
        /// </summary>
        public static class SeederRelativePath
        {
            public static string JsonPath = Path.Combine("Helpers", "SeedData");

            public static string CoursePath = Path.Combine(JsonPath, "courseData.json");
            public static string TopicPath = Path.Combine(JsonPath, "topicData.json");
            public static string AgeRangePath = Path.Combine(JsonPath, "ageRangeData.json");
            public static string SkillPath = Path.Combine(JsonPath, "skillData.json");
            public static string StandardPath = Path.Combine(JsonPath, "standardData.json");
            public static string LessonTopicPath = Path.Combine(JsonPath, "lessonTopicData.json");
            public static string LessonSkillPath = Path.Combine(JsonPath, "lessonSkillData.json");
            public static string LessonStandardPath = Path.Combine(
                JsonPath,
                "lessonStandardData.json"
            );
            public static string LessonPath = Path.Combine(JsonPath, "lessonData.json");
            public static string SectionPath = Path.Combine(JsonPath, "sectionData.json");
            public static string ContentPath = Path.Combine(JsonPath, "contentData.json");
            public static string QuizPath = Path.Combine(JsonPath, "quizData.json");
            public static string QuestionPath = Path.Combine(JsonPath, "questionData.json");
            public static string AnswerPath = Path.Combine(JsonPath, "answerData.json");
            public static string CurriculumPath = Path.Combine(JsonPath, "curriculumData.json");
            public static string ProgramLearningOutcomePath = Path.Combine(JsonPath, "programLearningOutcomeData.json");
            public static string CourseLearningOutcomePath = Path.Combine(JsonPath, "courseLearningOutcomeData.json");
            public static string LearningOutcomeMappingPath = Path.Combine(JsonPath, "learningOutcomeMappingData.json");
            public static string CurriculumCoursePath = Path.Combine(JsonPath, "curriculumCourseData.json");
            public static string AssignmentPath = Path.Combine(JsonPath, "assignmentData.json");
            public static string AssignmentQuestionPath = Path.Combine(JsonPath, "assignmentQuestionData.json");
            public static string RubricCriterionPath = Path.Combine(JsonPath, "rubricCriterionData.json");
        }
    }
}

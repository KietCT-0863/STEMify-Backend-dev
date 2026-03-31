using System.Text.Json.Serialization;

namespace Resource.Application.Models.ExportData
{
    public class ManifestModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = "STEM Education Team";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; }

        [JsonPropertyName("totalSlides")]
        public int TotalSlides { get; set; }

        [JsonPropertyName("slides")]
        public List<SlideManifest> Slides { get; set; } = new();

        [JsonPropertyName("assets")]
        public AssetsManifest Assets { get; set; } = new();

        [JsonPropertyName("metadata")]
        public LessonMetadata Metadata { get; set; } = new();
    }

    public class SlideManifest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "html";

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = "easy";

        [JsonPropertyName("hasQuiz")]
        public bool HasQuiz { get; set; }

        [JsonPropertyName("hasAnnotation")]
        public bool HasAnnotation { get; set; } = true;

        [JsonPropertyName("hasMedia")]
        public bool HasMedia { get; set; }

        [JsonPropertyName("interactions")]
        public List<InteractionModel> Interactions { get; set; } = new();
    }

    public class InteractionModel
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("config")]
        public Dictionary<string, object> Config { get; set; } = new();
    }

    public class AssetsManifest
    {
        [JsonPropertyName("images")]
        public List<AssetItem> Images { get; set; } = new();

        [JsonPropertyName("audio")]
        public List<AssetItem> Audio { get; set; } = new();

        [JsonPropertyName("video")]
        public List<AssetItem> Video { get; set; } = new();

        [JsonPropertyName("documents")]
        public List<AssetItem> Documents { get; set; } = new();

        [JsonPropertyName("fonts")]
        public List<AssetItem> Fonts { get; set; } = new();
    }

    public class AssetItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;
    }

    public class LessonMetadata
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = "vi";

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = "STEM";

        [JsonPropertyName("grade")]
        public string Grade { get; set; } = "K-12";

        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new();

        [JsonPropertyName("license")]
        public string License { get; set; } = "CC-BY-4.0";

        [JsonPropertyName("customMetadata")]
        public Dictionary<string, object> CustomMetadata { get; set; } = new();
    }

    public class QuizExportModel
    {
        [JsonPropertyName("quiz_id")]
        public int QuizId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("total_marks")]
        public double TotalMarks { get; set; }

        [JsonPropertyName("passing_marks")]
        public double PassingMarks { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("questions")]
        public List<QuestionModel> Questions { get; set; } = new();
    }

    public class QuestionModel
    {
        [JsonPropertyName("question_id")]
        public int QuestionId { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("question_type")]
        public string QuestionType { get; set; } = string.Empty;

        [JsonPropertyName("order_index")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("answers")]
        public List<AnswerModel> Answers { get; set; } = new();
    }

    public class AnswerModel
    {
        [JsonPropertyName("answer_id")]
        public int AnswerId { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("is_correct")]
        public bool IsCorrect { get; set; }
    }

    public class CourseManifestModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = "STEM Education Team";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; }

        [JsonPropertyName("totalLessons")]
        public int TotalLessons { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("lessons")]
        public List<LessonManifest> Lessons { get; set; } = new();

        [JsonPropertyName("assets")]
        public AssetsManifest Assets { get; set; } = new();

        [JsonPropertyName("metadata")]
        public CourseMetadata Metadata { get; set; } = new();
    }

    public class LessonManifest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("folder")]
        public string Folder { get; set; } = string.Empty;

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("totalSlides")]
        public int TotalSlides { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = "easy";

        [JsonPropertyName("learningOutcome")]
        public string LearningOutcome { get; set; } = string.Empty;

        [JsonPropertyName("hasMedia")]
        public bool HasMedia { get; set; }
    }

    public class CourseMetadata
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = "vi";

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = "STEM";

        [JsonPropertyName("grade")]
        public string Grade { get; set; } = "K-12";

        [JsonPropertyName("level")]
        public string Level { get; set; } = string.Empty;

        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new();

        [JsonPropertyName("license")]
        public string License { get; set; } = "CC-BY-4.0";

        [JsonPropertyName("customMetadata")]
        public Dictionary<string, object> CustomMetadata { get; set; } = new();
    }

    public class CourseExportModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public int Duration { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public List<string> Topics { get; set; } = new();
        public List<LessonExportModel> Lessons { get; set; } = new();
    }

    public class LessonExportModel
    {
        public int Id { get; set; }
        public string Level { get; set; } = string.Empty;
        public List<string> Topics { get; set; } = new();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LearningOutcome { get; set; } = string.Empty;
        public int Duration { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public List<SectionExportModel> Sections { get; set; } = new();
        public List<LessonAssetExportModel> Assets { get; set; } = new();
    }

    public class SectionExportModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public int Duration { get; set; }
        public List<ContentExportModel> Contents { get; set; } = new();
        public List<QuizExportDataModel> Quizzes { get; set; } = new();
    }

    public class ContentExportModel
    {
        public int Id { get; set; }
        public string ContentType { get; set; }
        public string ContentBody { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? FileUrl { get; set; }
    }

    public class LessonAssetExportModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string AssetUrl { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    public class QuizExportDataModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public double TotalMarks { get; set; }
        public double PassingMarks { get; set; }
        public int Duration { get; set; }
        public List<QuestionExportModel> Questions { get; set; } = new();
    }

    public class QuestionExportModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public List<AnswerExportModel> Answers { get; set; } = new();
    }

    public class AnswerExportModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}

using Microsoft.EntityFrameworkCore;
using Resource.Domain.Entities;
using Resource.Domain.Enums;
using System.Reflection;

namespace Resource.Infrastructure.Persistence;

public partial class ResourceDbContext : DbContext
{
    public ResourceDbContext() { }

    public ResourceDbContext(DbContextOptions<ResourceDbContext> options)
        : base(options) { }

    public virtual DbSet<AgeRange> AgeRanges { get; set; }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<LessonTopic> LessonTopics { get; set; }

    public virtual DbSet<LessonSkill> LessonSkills { get; set; }

    public virtual DbSet<LessonStandard> LessonStandards { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<Standard> Standards { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<Section> Sections { get; set; }

    public virtual DbSet<Content> Contents { get; set; }

    public virtual DbSet<Quiz> Quizzes { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Answer> Answers { get; set; }

    public virtual DbSet<CourseLearningOutcome> CourseLearningOutcomes { get; set; }

    public virtual DbSet<Curriculum> Curriculums { get; set; }

    public virtual DbSet<CurriculumCourse> CurriculumCourses { get; set; }

    public virtual DbSet<LearningOutcomeMapping> LearningOutcomeMappings { get; set; }

    public virtual DbSet<ProgramLearningOutcome> ProgramLearningOutcomes { get; set; }

    public virtual DbSet<LessonAsset> LessonAssets { get; set; }

    public virtual DbSet<LessonAssetTag> LessonAssetTags { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<Assignment> Assignments { get; set; }

    public virtual DbSet<AssignmentQuestion> AssignmentQuestions { get; set; }

    public virtual DbSet<RubricCriterion> RubricCriterions { get; set; }

    public virtual DbSet<CurriculumEmulation> CurriculumEmulations { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    var builder = new ConfigurationBuilder()
    //        .SetBasePath(Directory.GetCurrentDirectory())
    //        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

    //    IConfigurationRoot configurationRoot = builder.Build();
    //    optionsBuilder.UseNpgsql(configurationRoot.GetConnectionString("stemifyresource"));
    //}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity
                .Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'");

            entity
                .Property(e => e.LastModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'");

            entity.Property(e => e.Duration).HasDefaultValue(0);

            entity
                .Property(e => e.Status)
                .HasDefaultValue(CourseStatus.Draft)
                .HasConversion<string>();
            entity
                .Property(e => e.Level)
                .HasDefaultValue(CourseLevel.Beginner)
                .HasConversion<string>();

            entity
                .Property(m => m.Code).IsRequired();

            entity
                .HasIndex(e => e.Code)
                .IsUnique();
        });

        modelBuilder.Entity<AgeRange>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Standard>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Curriculum>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity
                .Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'");

            entity
                .Property(e => e.LastModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'");

            entity
                .Property(e => e.Status)
                .HasDefaultValue(CurriculumStatus.Draft)
                .HasConversion<string>();

            entity
                .Property(m => m.Code).IsRequired();

            entity
                .HasIndex(e => e.Code)
                .IsUnique();
        });

        modelBuilder.Entity<ProgramLearningOutcome>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<CourseLearningOutcome>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<LearningOutcomeMapping>(entity =>
        {
            entity.HasKey(e => new { e.CLOId, e.PLOId });

            entity
                .HasOne(e => e.CourseLearningOutcome)
                .WithMany(c => c.LearningOutcomeMappings)
                .HasForeignKey(e => e.CLOId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.ProgramLearningOutcome)
                .WithMany(a => a.LearningOutcomeMappings)
                .HasForeignKey(e => e.PLOId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CurriculumCourse>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity
                .HasOne(e => e.Course)
                .WithMany(c => c.CurriculumCourses)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.Curriculum)
                .WithMany(a => a.CurriculumCourses)
                .HasForeignKey(e => e.CurriculumId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint for {CourseId, CurriculumId}
            entity.HasIndex(e => new { e.CourseId, e.CurriculumId }).IsUnique();
        });

        modelBuilder.Entity<LessonTopic>(entity =>
        {
            entity.HasKey(e => new { e.LessonId, e.TopicId });

            entity
                .HasOne(e => e.Lesson)
                .WithMany(c => c.LessonTopics)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.Topic)
                .WithMany(a => a.LessonTopics)
                .HasForeignKey(e => e.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonSkill>(entity =>
        {
            entity.HasKey(e => new { e.LessonId, e.SkillId });

            entity
                .HasOne(e => e.Lesson)
                .WithMany(c => c.LessonSkills)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.Skill)
                .WithMany(a => a.LessonSkills)
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonStandard>(entity =>
        {
            entity.HasKey(e => new { e.LessonId, e.StandardId });

            entity
                .HasOne(e => e.Lesson)
                .WithMany(c => c.LessonStandards)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.Standard)
                .WithMany(a => a.LessonStandards)
                .HasForeignKey(e => e.StandardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity
                .Property(e => e.Status)
                .HasDefaultValue(LessonStatus.Draft)
                .HasConversion<string>();

            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity
                .Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'");

            entity
                .Property(e => e.LastModifiedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'");
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity
                .Property(e => e.Status)
                .HasDefaultValue(SectionStatus.Draft)
                .HasConversion<string>();

            entity.Property(e => e.IsVisibleToStudent)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<Content>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity
                .Property(e => e.Status)
                .HasDefaultValue(ContentStatus.Draft)
                .HasConversion<string>();

            entity
                .Property(x => x.ContentType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue(ContentType.Text);
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.TotalMarks).HasDefaultValue(100);

            entity.Property(e => e.PassingMarks).HasDefaultValue(80);

            entity.Property(e => e.TimeLimitInMinutes).IsRequired(false);

            entity.Property(e => e.Description).IsRequired(false);
            entity.Property(e => e.Title).IsRequired(true);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity
                .Property(e => e.QuestionType)
                .HasDefaultValue(Domain.Enums.QuestionType.MultipleChoice)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Content).IsRequired(true);
            entity.Property(e => e.AnswerExplanation).IsRequired(false);
            entity.Property(e => e.FileUrl).IsRequired(false);
        });

        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Content).IsRequired(true);
            entity.Property(e => e.IsCorrect).HasDefaultValue(false);
        });
    }
}

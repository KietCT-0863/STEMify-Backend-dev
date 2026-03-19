using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.UnitTests.Repositories;

/// <summary>
/// Base class for repository integration tests
/// Provides in-memory database setup and common test utilities
/// </summary>
public abstract class RepositoryTestBase
{
    protected ApplicationDbContext Context { get; private set; } = null!;
    protected UserManager<ApplicationUser> UserManager { get; private set; } = null!;
    protected ServiceProvider ServiceProvider { get; private set; } = null!;

    [SetUp]
    public virtual async Task SetUp()
    {
        var services = new ServiceCollection();

        // Configure in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        );

        // Configure Identity
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                // Disable password requirements for testing
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 1;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        ServiceProvider = services.BuildServiceProvider();

        Context = ServiceProvider.GetRequiredService<ApplicationDbContext>();
        UserManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Ensure database is created
        await Context.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public virtual async Task TearDown()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
        ServiceProvider.Dispose();
    }

    /// <summary>
    /// Create a test teacher with specified properties
    /// </summary>
    protected static Teacher CreateTestTeacher(
        string email = "teacher@test.com",
        string userName = "teacher1",
        string firstName = "John",
        string lastName = "Doe",
        string? bio = null,
        string? specialization = null
    )
    {
        return Teacher.Create(
            Guid.NewGuid(),
            email,
            userName,
            firstName,
            lastName,
            bio,
            specialization
        );
    }

    /// <summary>
    /// Create a test student with specified properties
    /// </summary>
    protected static Student CreateTestStudent(
        string email = "student@test.com",
        string userName = "student1",
        string firstName = "Jane",
        string lastName = "Smith",
        DateTime? dateOfBirth = null,
        string? bio = null,
        string? major = null
    )
    {
        return Student.Create(
            Guid.NewGuid(),
            email,
            userName,
            firstName,
            lastName,
            dateOfBirth ?? DateTime.Today.AddYears(-20),
            bio,
            major
        );
    }

    /// <summary>
    /// Seed the database with test data
    /// </summary>
    protected async Task SeedTestDataAsync()
    {
        // Create test teachers
        var teacher1 = CreateTestTeacher(
            "teacher1@test.com",
            "teacher1",
            "John",
            "Doe",
            "Experienced math teacher",
            "Mathematics"
        );

        var teacher2 = CreateTestTeacher(
            "teacher2@test.com",
            "teacher2",
            "Jane",
            "Smith",
            "Computer science expert",
            "Computer Science"
        );

        var teacher3 = CreateTestTeacher(
            "teacher3@test.com",
            "teacher3",
            "Bob",
            "Johnson",
            "Physics enthusiast",
            "Physics"
        );

        // Create test students
        var student1 = CreateTestStudent(
            "student1@test.com",
            "student1",
            "Alice",
            "Brown",
            DateTime.Today.AddYears(-20),
            "Passionate about programming",
            "Computer Science"
        );

        var student2 = CreateTestStudent(
            "student2@test.com",
            "student2",
            "Charlie",
            "Wilson",
            DateTime.Today.AddYears(-19),
            "Math lover",
            "Mathematics"
        );

        var student3 = CreateTestStudent(
            "student3@test.com",
            "student3",
            "Diana",
            "Davis",
            DateTime.Today.AddYears(-21),
            "Future engineer",
            "Engineering"
        );

        var student4 = CreateTestStudent(
            "student4@test.com",
            "student4",
            "Eve",
            "Miller",
            DateTime.Today.AddYears(-17),
            "High school student",
            null // No major yet
        );

        // Add to context
        Context.Teachers.AddRange(teacher1, teacher2, teacher3);
        Context.Students.AddRange(student1, student2, student3, student4);

        // Activate some users
        teacher1.Activate();
        teacher2.Activate();
        student1.Activate();
        student2.Activate();
        student3.Activate();

        await Context.SaveChangesAsync();
    }
}

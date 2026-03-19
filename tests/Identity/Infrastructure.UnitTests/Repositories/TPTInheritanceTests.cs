using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UnitTests.Repositories;

/// <summary>
/// Integration tests specifically focused on TPT (Table Per Type) inheritance behavior
/// </summary>
[TestFixture]
public class TPTInheritanceTests : RepositoryTestBase
{
    private UserRepositoryBase<ApplicationUser> _baseRepository = null!;
    private TeacherRepository _teacherRepository = null!;
    private StudentRepository _studentRepository = null!;

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        _baseRepository = new UserRepositoryBase<ApplicationUser>(Context, UserManager);
        _teacherRepository = new TeacherRepository(Context, UserManager);
        _studentRepository = new StudentRepository(Context, UserManager);
    }

    #region TPT Schema Validation

    [Test]
    public async Task TPTSchema_ShouldHaveCorrectTableStructure()
    {
        // Arrange & Act
        await SeedTestDataAsync();

        // Assert - Verify that data is stored in separate tables
        var usersCount = await Context
            .Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM AspNetUsers")
            .FirstAsync();
        var teachersCount = await Context
            .Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM Teachers")
            .FirstAsync();
        var studentsCount = await Context
            .Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM Students")
            .FirstAsync();

        usersCount.Should().Be(teachersCount + studentsCount);
        teachersCount.Should().BeGreaterThan(0);
        studentsCount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task TPTInheritance_ShouldMaintainReferentialIntegrity()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Get all users from base table
        var allUsers = await Context.Users.ToListAsync();
        var teachers = await Context.Teachers.ToListAsync();
        var students = await Context.Students.ToListAsync();

        // Assert - Every teacher and student should exist in base Users table
        foreach (var teacher in teachers)
        {
            allUsers.Should().Contain(u => u.Id == teacher.Id);
        }

        foreach (var student in students)
        {
            allUsers.Should().Contain(u => u.Id == student.Id);
        }

        // Total count should match
        allUsers.Count.Should().Be(teachers.Count + students.Count);
    }

    #endregion

    #region Polymorphic Queries

    [Test]
    public async Task PolymorphicQuery_ShouldReturnCorrectDerivedTypes()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Query base type and check derived types
        var allUsers = await Context.Users.ToListAsync();
        var teacherUsers = allUsers.OfType<Teacher>().ToList();
        var studentUsers = allUsers.OfType<Student>().ToList();

        // Assert
        teacherUsers.Should().NotBeEmpty();
        studentUsers.Should().NotBeEmpty();

        teacherUsers
            .Should()
            .AllSatisfy(t =>
            {
                t.Role.Should().Be(UserRole.Teacher);
                t.Should().BeOfType<Teacher>();
            });

        studentUsers
            .Should()
            .AllSatisfy(s =>
            {
                s.Role.Should().Be(UserRole.Student);
                s.Should().BeOfType<Student>();
            });
    }

    [Test]
    public async Task PolymorphicQuery_WithComplexFiltering_ShouldWorkCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Complex polymorphic query
        var activeUsersWithDetails = await Context
            .Users.Where(u => u.Status == UserStatus.Active)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Role,
                u.FullName,
                TypeSpecificData = u is Teacher ? $"Teacher: {((Teacher)u).Specialization}"
                : u is Student
                    ? $"Student: {((Student)u).Major} (Age: {EF.Functions.DateDiffYear(((Student)u).DateOfBirth, DateTime.Today)})"
                : "Unknown",
            })
            .ToListAsync();

        // Assert
        activeUsersWithDetails.Should().NotBeEmpty();
        activeUsersWithDetails
            .Should()
            .AllSatisfy(u =>
            {
                u.FullName.Should().NotBeNullOrEmpty();
                u.TypeSpecificData.Should().NotBe("Unknown");
                u.TypeSpecificData.Should().StartWithAny("Teacher:", "Student:");
            });
    }

    [Test]
    public async Task PolymorphicQuery_WithJoins_ShouldMaintainTypeInformation()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Query with joins that should preserve type information
        var userTypeStatistics = await Context
            .Users.GroupBy(u => u.Role)
            .Select(g => new
            {
                UserType = g.Key,
                TotalCount = g.Count(),
                ActiveCount = g.Count(u => u.Status == UserStatus.Active),
                InactiveCount = g.Count(u => u.Status != UserStatus.Active),
                SampleUsers = g.Take(2)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                        SpecificInfo = u is Teacher
                            ? ((Teacher)u).Specialization
                            : ((Student)u).Major,
                    })
                    .ToList(),
            })
            .ToListAsync();

        // Assert
        userTypeStatistics.Should().HaveCount(2); // Teacher and Student

        var teacherStats = userTypeStatistics.First(s => s.UserType == UserRole.Teacher);
        var studentStats = userTypeStatistics.First(s => s.UserType == UserRole.Student);

        teacherStats.TotalCount.Should().BeGreaterThan(0);
        studentStats.TotalCount.Should().BeGreaterThan(0);

        teacherStats.SampleUsers.Should().AllSatisfy(u => u.SpecificInfo.Should().NotBeNull());
        // Students might have null major, so we don't assert that
    }

    #endregion

    #region Cross-Type Operations

    [Test]
    public async Task CrossTypeQuery_ShouldAllowComparisonsBetweenDerivedTypes()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Query that compares teachers and students
        var userComparison = await Context
            .Users.Where(u => u.Status == UserStatus.Active)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Role,
                u.CreatedAt,
                IsTeacher = u is Teacher,
                IsStudent = u is Student,
                HasSpecialization = u is Teacher
                    && !string.IsNullOrEmpty(((Teacher)u).Specialization),
                HasMajor = u is Student && !string.IsNullOrEmpty(((Student)u).Major),
                Age = u is Student
                    ? EF.Functions.DateDiffYear(((Student)u).DateOfBirth, DateTime.Today)
                    : (int?)null,
            })
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

        // Assert
        userComparison.Should().NotBeEmpty();
        userComparison
            .Should()
            .AllSatisfy(u =>
            {
                (u.IsTeacher || u.IsStudent).Should().BeTrue();
                (u.IsTeacher && u.IsStudent).Should().BeFalse(); // Can't be both
            });

        var teachers = userComparison.Where(u => u.IsTeacher).ToList();
        var students = userComparison.Where(u => u.IsStudent).ToList();

        teachers.Should().AllSatisfy(t => t.Age.Should().BeNull()); // Teachers don't have age
        students.Should().AllSatisfy(s => s.Age.Should().HaveValue()); // Students should have age
    }

    [Test]
    public async Task MixedRepositoryOperations_ShouldMaintainDataIntegrity()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Perform operations using different repositories
        var teacher = await _teacherRepository.GetActiveTeachersAsync();
        var student = await _studentRepository.GetActiveStudentsAsync();
        var allUsers = await _baseRepository.GetActiveUsersAsync();

        // Modify entities through specific repositories
        var firstTeacher = teacher.First();
        firstTeacher.UpdateSpecialization("Updated through TeacherRepository");
        await _teacherRepository.UpdateAsync(firstTeacher);

        var firstStudent = student.First();
        firstStudent.UpdateMajor("Updated through StudentRepository");
        await _studentRepository.UpdateAsync(firstStudent);

        // Assert - Changes should be visible through base repository
        var updatedTeacher = await _baseRepository.GetByIdAsync(firstTeacher.Id);
        var updatedStudent = await _baseRepository.GetByIdAsync(firstStudent.Id);

        updatedTeacher.Should().BeOfType<Teacher>();
        ((Teacher)updatedTeacher!).Specialization.Should().Be("Updated through TeacherRepository");

        updatedStudent.Should().BeOfType<Student>();
        ((Student)updatedStudent!).Major.Should().Be("Updated through StudentRepository");

        // Total active count should remain consistent
        var updatedAllUsers = await _baseRepository.GetActiveUsersAsync();
        updatedAllUsers.Count.Should().Be(allUsers.Count);
    }

    #endregion

    #region Performance Tests

    [Test]
    public async Task TPTQueries_ShouldPerformEfficientlyWithLargeDataset()
    {
        // Arrange - Create large dataset
        var teachers = new List<Teacher>();
        var students = new List<Student>();

        for (int i = 0; i < 50; i++)
        {
            teachers.Add(
                CreateTestTeacher(
                    $"perf_teacher_{i}@test.com",
                    $"perf_teacher_{i}",
                    $"Teacher{i}",
                    "Performance",
                    $"Bio {i}",
                    $"Subject {i % 10}"
                )
            );

            students.Add(
                CreateTestStudent(
                    $"perf_student_{i}@test.com",
                    $"perf_student_{i}",
                    $"Student{i}",
                    "Performance",
                    DateTime.Today.AddYears(-(18 + i % 10)),
                    $"Bio {i}",
                    $"Major {i % 5}"
                )
            );
        }

        Context.Teachers.AddRange(teachers);
        Context.Students.AddRange(students);
        await Context.SaveChangesAsync();

        // Act & Assert - Multiple query types should perform well
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Base polymorphic query
        var allUsers = await Context.Users.ToListAsync();
        var polymorphicTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();

        // Specific type queries
        var teachersBySpec = await Context
            .Teachers.Where(t => t.Specialization!.Contains("Subject"))
            .ToListAsync();
        var specificQueryTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();

        // Complex join query
        var complexQuery = await Context
            .Users.Where(u => u.Status == UserStatus.Pending)
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync();
        var complexQueryTime = stopwatch.ElapsedMilliseconds;

        // Assert performance
        allUsers.Should().HaveCount(100); // 50 teachers + 50 students
        polymorphicTime.Should().BeLessThan(2000);
        specificQueryTime.Should().BeLessThan(1000);
        complexQueryTime.Should().BeLessThan(1000);
    }

    #endregion

    #region Error Handling

    [Test]
    public async Task TPTOperations_ShouldHandleConstraintViolationsCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();
        var existingTeacher = Context.Teachers.First();

        // Act & Assert - Duplicate email should fail
        var duplicateTeacher = CreateTestTeacher(
            existingTeacher.Email!,
            "different_username",
            "Different",
            "Name",
            "Bio",
            "Subject"
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _teacherRepository.AddAsync(duplicateTeacher)
        );
        exception.Message.Should().Contain("email");
    }

    [Test]
    public async Task TPTDeletion_ShouldCascadeCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();
        var teacherId = teacher.Id;

        // Act - Delete through specific repository
        await _teacherRepository.DeleteAsync(teacher);

        // Assert - Should be deleted from both base and derived tables
        var deletedFromBase = await Context.Users.FindAsync(teacherId);
        var deletedFromDerived = await Context.Teachers.FindAsync(teacherId);

        deletedFromBase.Should().BeNull();
        deletedFromDerived.Should().BeNull();
    }

    #endregion
}

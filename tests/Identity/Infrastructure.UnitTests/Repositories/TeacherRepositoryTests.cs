using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UnitTests.Repositories;

[TestFixture]
public class TeacherRepositoryTests : RepositoryTestBase
{
    private TeacherRepository _repository = null!;

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        _repository = new TeacherRepository(Context, UserManager);
    }

    #region Teacher-Specific Query Methods

    [Test]
    public async Task GetBySpecializationAsync_WithExistingSpecialization_ShouldReturnMatchingTeachers()
    {
        // Arrange
        await SeedTestDataAsync();
        var specialization = "Mathematics";

        // Act
        var result = await _repository.GetBySpecializationAsync(specialization);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(t => t.Specialization.Should().Be(specialization));
    }

    [Test]
    public async Task GetBySpecializationAsync_WithNonExistingSpecialization_ShouldReturnEmpty()
    {
        // Arrange
        await SeedTestDataAsync();
        var specialization = "NonExistentSubject";

        // Act
        var result = await _repository.GetBySpecializationAsync(specialization);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetActiveTeachersAsync_ShouldReturnOnlyActiveTeachers()
    {
        // Arrange
        await SeedTestDataAsync();
        var expectedActiveCount = Context.Teachers.Count(t => t.Status == UserStatus.Active);

        // Act
        var result = await _repository.GetActiveTeachersAsync();

        // Assert
        result.Should().HaveCount(expectedActiveCount);
        result.Should().AllSatisfy(t => t.Status.Should().Be(UserStatus.Active));
    }

    [Test]
    public async Task GetTeachersWithSpecializationAsync_ShouldReturnTeachersWithNonEmptySpecialization()
    {
        // Arrange
        await SeedTestDataAsync();

        // Add a teacher without specialization
        var teacherWithoutSpec = CreateTestTeacher(
            "nospec@test.com",
            "nospec",
            "No",
            "Specialization",
            "Bio without specialization",
            null
        );
        Context.Teachers.Add(teacherWithoutSpec);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTeachersWithSpecializationAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(t => t.Specialization.Should().NotBeNullOrWhiteSpace());
        result.Should().NotContain(t => t.Id == teacherWithoutSpec.Id);
    }

    [Test]
    public async Task GetTeachersWithoutSpecializationAsync_ShouldReturnTeachersWithEmptySpecialization()
    {
        // Arrange
        await SeedTestDataAsync();

        // Add teachers without specialization
        var teacher1 = CreateTestTeacher("nospec1@test.com", "nospec1", "No", "Spec1", "Bio", null);
        var teacher2 = CreateTestTeacher("nospec2@test.com", "nospec2", "No", "Spec2", "Bio", "");
        var teacher3 = CreateTestTeacher(
            "nospec3@test.com",
            "nospec3",
            "No",
            "Spec3",
            "Bio",
            "   "
        );

        Context.Teachers.AddRange(teacher1, teacher2, teacher3);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTeachersWithoutSpecializationAsync();

        // Assert
        result.Should().HaveCount(3);
        result
            .Should()
            .AllSatisfy(t => string.IsNullOrWhiteSpace(t.Specialization).Should().BeTrue());
        result.Should().Contain(t => t.Id == teacher1.Id);
        result.Should().Contain(t => t.Id == teacher2.Id);
        result.Should().Contain(t => t.Id == teacher3.Id);
    }

    #endregion

    #region Database Constraint Enforcement Tests

    [Test]
    public async Task AddTeacher_WithDuplicateEmail_ShouldThrowException()
    {
        // Arrange
        await SeedTestDataAsync();
        var existingTeacher = Context.Teachers.First();

        var duplicateEmailTeacher = CreateTestTeacher(
            existingTeacher.Email!,
            "newusername",
            "New",
            "Teacher",
            "Bio",
            "Subject"
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.AddAsync(duplicateEmailTeacher)
        );
        exception.Message.Should().Contain("email");
    }

    [Test]
    public async Task AddTeacher_WithDuplicateUserName_ShouldThrowException()
    {
        // Arrange
        await SeedTestDataAsync();
        var existingTeacher = Context.Teachers.First();

        var duplicateUserNameTeacher = CreateTestTeacher(
            "newemail@test.com",
            existingTeacher.UserName!,
            "New",
            "Teacher",
            "Bio",
            "Subject"
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.AddAsync(duplicateUserNameTeacher)
        );
        exception.Message.Should().Contain("username");
    }

    [Test]
    public async Task UpdateTeacher_WithValidSpecialization_ShouldUpdateSuccessfully()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();
        var newSpecialization = "Updated Mathematics";

        // Act
        teacher.UpdateSpecialization(newSpecialization);
        await _repository.UpdateAsync(teacher);

        // Assert
        var updatedTeacher = await _repository.GetByIdAsync(teacher.Id);
        updatedTeacher.Should().NotBeNull();
        updatedTeacher!.Specialization.Should().Be(newSpecialization);
    }

    [Test]
    public async Task UpdateTeacher_WithEmptySpecialization_ShouldUpdateSuccessfully()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();

        // Act
        teacher.UpdateSpecialization(null);
        await _repository.UpdateAsync(teacher);

        // Assert
        var updatedTeacher = await _repository.GetByIdAsync(teacher.Id);
        updatedTeacher.Should().NotBeNull();
        updatedTeacher!.Specialization.Should().BeNull();
    }

    #endregion

    #region Performance and Index Tests

    [Test]
    public async Task GetBySpecializationAsync_WithManyTeachers_ShouldPerformEfficiently()
    {
        // Arrange - Create many teachers with same specialization
        var specialization = "Performance Test Subject";
        var teachers = new List<Teacher>();

        for (int i = 0; i < 100; i++)
        {
            teachers.Add(
                CreateTestTeacher(
                    $"perftest{i}@test.com",
                    $"perftest{i}",
                    $"Teacher{i}",
                    "Performance",
                    $"Bio {i}",
                    specialization
                )
            );
        }

        Context.Teachers.AddRange(teachers);
        await Context.SaveChangesAsync();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _repository.GetBySpecializationAsync(specialization);
        stopwatch.Stop();

        // Assert
        result.Should().HaveCount(100);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Test]
    public async Task GetActiveTeachersAsync_WithManyTeachers_ShouldPerformEfficiently()
    {
        // Arrange - Create many active teachers
        var teachers = new List<Teacher>();

        for (int i = 0; i < 100; i++)
        {
            var teacher = CreateTestTeacher(
                $"active{i}@test.com",
                $"active{i}",
                $"Active{i}",
                "Teacher",
                $"Bio {i}",
                $"Subject {i % 10}" // 10 different subjects
            );
            teacher.Activate();
            teachers.Add(teacher);
        }

        Context.Teachers.AddRange(teachers);
        await Context.SaveChangesAsync();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _repository.GetActiveTeachersAsync();
        stopwatch.Stop();

        // Assert
        result.Should().HaveCount(100);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    #endregion

    #region TPT Polymorphic Query Tests

    [Test]
    public async Task GetTeacherAsApplicationUser_ShouldMaintainTeacherProperties()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();

        // Act - Query as base type but cast back to derived type
        var userAsBase = await Context.Users.FirstOrDefaultAsync(u => u.Id == teacher.Id);
        var userAsTeacher = userAsBase as Teacher;

        // Assert
        userAsBase.Should().NotBeNull();
        userAsTeacher.Should().NotBeNull();
        userAsTeacher!.Specialization.Should().Be(teacher.Specialization);
        userAsTeacher.Bio.Should().Be(teacher.Bio);
        userAsTeacher.Role.Should().Be(UserRole.Teacher);
    }

    [Test]
    public async Task QueryAllUsers_ShouldIncludeTeachersWithCorrectType()
    {
        // Arrange
        await SeedTestDataAsync();
        var expectedTeacherCount = Context.Teachers.Count();

        // Act
        var allUsers = await Context.Users.ToListAsync();
        var teachersFromAllUsers = allUsers.OfType<Teacher>().ToList();

        // Assert
        teachersFromAllUsers.Should().HaveCount(expectedTeacherCount);
        teachersFromAllUsers
            .Should()
            .AllSatisfy(t =>
            {
                t.Role.Should().Be(UserRole.Teacher);
                t.Specialization.Should().NotBeNull();
            });
    }

    [Test]
    public async Task ComplexPolymorphicQuery_ShouldWorkCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Complex query that joins base and derived tables
        var teacherSummary = await Context
            .Users.OfType<Teacher>()
            .Where(t => t.Status == UserStatus.Active)
            .Where(t => !string.IsNullOrEmpty(t.Specialization))
            .GroupBy(t => t.Specialization)
            .Select(g => new
            {
                Specialization = g.Key,
                Count = g.Count(),
                Teachers = g.Select(t => new
                    {
                        t.Id,
                        t.FullName,
                        t.Email,
                    })
                    .ToList(),
            })
            .ToListAsync();

        // Assert
        teacherSummary.Should().NotBeEmpty();
        teacherSummary
            .Should()
            .AllSatisfy(summary =>
            {
                summary.Specialization.Should().NotBeNullOrEmpty();
                summary.Count.Should().BeGreaterThan(0);
                summary.Teachers.Should().NotBeEmpty();
                summary
                    .Teachers.Should()
                    .AllSatisfy(t =>
                    {
                        t.FullName.Should().NotBeNullOrEmpty();
                        t.Email.Should().NotBeNullOrEmpty();
                    });
            });
    }

    #endregion
}

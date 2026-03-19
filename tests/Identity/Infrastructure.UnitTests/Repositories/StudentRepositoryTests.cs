using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UnitTests.Repositories;

[TestFixture]
public class StudentRepositoryTests : RepositoryTestBase
{
    private StudentRepository _repository = null!;

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        _repository = new StudentRepository(Context, UserManager);
    }

    #region Student-Specific Query Methods

    [Test]
    public async Task GetByMajorAsync_WithExistingMajor_ShouldReturnMatchingStudents()
    {
        // Arrange
        await SeedTestDataAsync();
        var major = "Computer Science";

        // Act
        var result = await _repository.GetByMajorAsync(major);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(s => s.Major.Should().Be(major));
    }

    [Test]
    public async Task GetByMajorAsync_WithNonExistingMajor_ShouldReturnEmpty()
    {
        // Arrange
        await SeedTestDataAsync();
        var major = "NonExistentMajor";

        // Act
        var result = await _repository.GetByMajorAsync(major);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetActiveStudentsAsync_ShouldReturnOnlyActiveStudents()
    {
        // Arrange
        await SeedTestDataAsync();
        var expectedActiveCount = Context.Students.Count(s => s.Status == UserStatus.Active);

        // Act
        var result = await _repository.GetActiveStudentsAsync();

        // Assert
        result.Should().HaveCount(expectedActiveCount);
        result.Should().AllSatisfy(s => s.Status.Should().Be(UserStatus.Active));
    }

    [Test]
    public async Task GetStudentsByAgeRangeAsync_ShouldReturnStudentsInRange()
    {
        // Arrange
        await SeedTestDataAsync();
        var minAge = 18;
        var maxAge = 25;

        // Act
        var result = await _repository.GetStudentsByAgeRangeAsync(minAge, maxAge);

        // Assert
        result.Should().NotBeEmpty();
        result
            .Should()
            .AllSatisfy(s =>
            {
                var age = s.GetAge();
                age.Should().BeGreaterOrEqualTo(minAge);
                age.Should().BeLessOrEqualTo(maxAge);
            });
    }

    [Test]
    public async Task GetStudentsByAgeRangeAsync_WithNoStudentsInRange_ShouldReturnEmpty()
    {
        // Arrange
        await SeedTestDataAsync();
        var minAge = 50;
        var maxAge = 60;

        // Act
        var result = await _repository.GetStudentsByAgeRangeAsync(minAge, maxAge);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetStudentsWithMajorAsync_ShouldReturnStudentsWithNonEmptyMajor()
    {
        // Arrange
        await SeedTestDataAsync();

        // Add a student without major
        var studentWithoutMajor = CreateTestStudent(
            "nomajor@test.com",
            "nomajor",
            "No",
            "Major",
            DateTime.Today.AddYears(-20),
            "Bio without major",
            null
        );
        Context.Students.Add(studentWithoutMajor);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetStudentsWithMajorAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(s => s.Major.Should().NotBeNullOrWhiteSpace());
        result.Should().NotContain(s => s.Id == studentWithoutMajor.Id);
    }

    [Test]
    public async Task GetStudentsWithoutMajorAsync_ShouldReturnStudentsWithEmptyMajor()
    {
        // Arrange
        await SeedTestDataAsync();

        // Add students without major
        var student1 = CreateTestStudent(
            "nomajor1@test.com",
            "nomajor1",
            "No",
            "Major1",
            DateTime.Today.AddYears(-19),
            "Bio",
            null
        );
        var student2 = CreateTestStudent(
            "nomajor2@test.com",
            "nomajor2",
            "No",
            "Major2",
            DateTime.Today.AddYears(-20),
            "Bio",
            ""
        );
        var student3 = CreateTestStudent(
            "nomajor3@test.com",
            "nomajor3",
            "No",
            "Major3",
            DateTime.Today.AddYears(-18),
            "Bio",
            "   "
        );

        Context.Students.AddRange(student1, student2, student3);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetStudentsWithoutMajorAsync();

        // Assert
        result.Should().HaveCount(4); // 3 new + 1 from seed data
        result.Should().AllSatisfy(s => string.IsNullOrWhiteSpace(s.Major).Should().BeTrue());
        result.Should().Contain(s => s.Id == student1.Id);
        result.Should().Contain(s => s.Id == student2.Id);
        result.Should().Contain(s => s.Id == student3.Id);
    }

    [Test]
    public async Task GetYoungStudentsAsync_ShouldReturnStudentsUnder18()
    {
        // Arrange
        await SeedTestDataAsync();

        // Add young students
        var youngStudent1 = CreateTestStudent(
            "young1@test.com",
            "young1",
            "Young",
            "Student1",
            DateTime.Today.AddYears(-16),
            "Young student bio",
            null
        );
        var youngStudent2 = CreateTestStudent(
            "young2@test.com",
            "young2",
            "Young",
            "Student2",
            DateTime.Today.AddYears(-15),
            "Another young student",
            "High School"
        );

        Context.Students.AddRange(youngStudent1, youngStudent2);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetYoungStudentsAsync();

        // Assert
        result.Should().HaveCount(3); // 2 new + 1 from seed data (17 years old)
        result.Should().AllSatisfy(s => s.GetAge().Should().BeLessThan(18));
    }

    #endregion

    #region Database Constraint Enforcement Tests

    [Test]
    public async Task AddStudent_WithDuplicateEmail_ShouldThrowException()
    {
        // Arrange
        await SeedTestDataAsync();
        var existingStudent = Context.Students.First();

        var duplicateEmailStudent = CreateTestStudent(
            existingStudent.Email!,
            "newusername",
            "New",
            "Student",
            DateTime.Today.AddYears(-20),
            "Bio",
            "Major"
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.AddAsync(duplicateEmailStudent)
        );
        exception.Message.Should().Contain("email");
    }

    [Test]
    public async Task AddStudent_WithDuplicateUserName_ShouldThrowException()
    {
        // Arrange
        await SeedTestDataAsync();
        var existingStudent = Context.Students.First();

        var duplicateUserNameStudent = CreateTestStudent(
            "newemail@test.com",
            existingStudent.UserName!,
            "New",
            "Student",
            DateTime.Today.AddYears(-20),
            "Bio",
            "Major"
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.AddAsync(duplicateUserNameStudent)
        );
        exception.Message.Should().Contain("username");
    }

    [Test]
    public async Task UpdateStudent_WithValidMajor_ShouldUpdateSuccessfully()
    {
        // Arrange
        await SeedTestDataAsync();
        var student = Context.Students.First();
        var newMajor = "Updated Computer Science";

        // Act
        student.UpdateMajor(newMajor);
        await _repository.UpdateAsync(student);

        // Assert
        var updatedStudent = await _repository.GetByIdAsync(student.Id);
        updatedStudent.Should().NotBeNull();
        updatedStudent!.Major.Should().Be(newMajor);
    }

    [Test]
    public async Task UpdateStudent_WithEmptyMajor_ShouldUpdateSuccessfully()
    {
        // Arrange
        await SeedTestDataAsync();
        var student = Context.Students.First();

        // Act
        student.UpdateMajor(null);
        await _repository.UpdateAsync(student);

        // Assert
        var updatedStudent = await _repository.GetByIdAsync(student.Id);
        updatedStudent.Should().NotBeNull();
        updatedStudent!.Major.Should().BeNull();
    }

    [Test]
    public async Task AddStudent_WithFutureDateOfBirth_ShouldThrowException()
    {
        // Arrange
        var futureDate = DateTime.Today.AddYears(1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateTestStudent(
                "future@test.com",
                "future",
                "Future",
                "Student",
                futureDate,
                "Bio",
                "Major"
            )
        );
        exception.Message.Should().Contain("future");
    }

    #endregion

    #region Performance and Index Tests

    [Test]
    public async Task GetByMajorAsync_WithManyStudents_ShouldPerformEfficiently()
    {
        // Arrange - Create many students with same major
        var major = "Performance Test Major";
        var students = new List<Student>();

        for (int i = 0; i < 100; i++)
        {
            students.Add(
                CreateTestStudent(
                    $"perftest{i}@test.com",
                    $"perftest{i}",
                    $"Student{i}",
                    "Performance",
                    DateTime.Today.AddYears(-20),
                    $"Bio {i}",
                    major
                )
            );
        }

        Context.Students.AddRange(students);
        await Context.SaveChangesAsync();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _repository.GetByMajorAsync(major);
        stopwatch.Stop();

        // Assert
        result.Should().HaveCount(100);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Test]
    public async Task GetStudentsByAgeRangeAsync_WithManyStudents_ShouldPerformEfficiently()
    {
        // Arrange - Create many students with different ages
        var students = new List<Student>();

        for (int i = 0; i < 100; i++)
        {
            var age = 18 + (i % 10); // Ages from 18 to 27
            students.Add(
                CreateTestStudent(
                    $"age{i}@test.com",
                    $"age{i}",
                    $"Age{i}",
                    "Student",
                    DateTime.Today.AddYears(-age),
                    $"Bio {i}",
                    $"Major {i % 5}" // 5 different majors
                )
            );
        }

        Context.Students.AddRange(students);
        await Context.SaveChangesAsync();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _repository.GetStudentsByAgeRangeAsync(20, 25);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    #endregion

    #region TPT Polymorphic Query Tests

    [Test]
    public async Task GetStudentAsApplicationUser_ShouldMaintainStudentProperties()
    {
        // Arrange
        await SeedTestDataAsync();
        var student = Context.Students.First();

        // Act - Query as base type but cast back to derived type
        var userAsBase = await Context.Users.FirstOrDefaultAsync(u => u.Id == student.Id);
        var userAsStudent = userAsBase as Student;

        // Assert
        userAsBase.Should().NotBeNull();
        userAsStudent.Should().NotBeNull();
        userAsStudent!.Major.Should().Be(student.Major);
        userAsStudent.DateOfBirth.Should().Be(student.DateOfBirth);
        userAsStudent.Role.Should().Be(UserRole.Student);
        userAsStudent.GetAge().Should().Be(student.GetAge());
    }

    [Test]
    public async Task QueryAllUsers_ShouldIncludeStudentsWithCorrectType()
    {
        // Arrange
        await SeedTestDataAsync();
        var expectedStudentCount = Context.Students.Count();

        // Act
        var allUsers = await Context.Users.ToListAsync();
        var studentsFromAllUsers = allUsers.OfType<Student>().ToList();

        // Assert
        studentsFromAllUsers.Should().HaveCount(expectedStudentCount);
        studentsFromAllUsers
            .Should()
            .AllSatisfy(s =>
            {
                s.Role.Should().Be(UserRole.Student);
                s.DateOfBirth.Should().BeBefore(DateTime.Today);
                s.GetAge().Should().BeGreaterThan(0);
            });
    }

    [Test]
    public async Task ComplexPolymorphicQuery_ShouldWorkCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Complex query that joins base and derived tables
        var studentSummary = await Context
            .Users.OfType<Student>()
            .Where(s => s.Status == UserStatus.Active)
            .Where(s => !string.IsNullOrEmpty(s.Major))
            .GroupBy(s => s.Major)
            .Select(g => new
            {
                Major = g.Key,
                Count = g.Count(),
                AverageAge = g.Average(s =>
                    EF.Functions.DateDiffYear(s.DateOfBirth, DateTime.Today)
                ),
                Students = g.Select(s => new
                    {
                        s.Id,
                        s.FullName,
                        s.Email,
                        Age = s.GetAge(),
                    })
                    .ToList(),
            })
            .ToListAsync();

        // Assert
        studentSummary.Should().NotBeEmpty();
        studentSummary
            .Should()
            .AllSatisfy(summary =>
            {
                summary.Major.Should().NotBeNullOrEmpty();
                summary.Count.Should().BeGreaterThan(0);
                summary.AverageAge.Should().BeGreaterThan(0);
                summary.Students.Should().NotBeEmpty();
                summary
                    .Students.Should()
                    .AllSatisfy(s =>
                    {
                        s.FullName.Should().NotBeNullOrEmpty();
                        s.Email.Should().NotBeNullOrEmpty();
                        s.Age.Should().BeGreaterThan(0);
                    });
            });
    }

    [Test]
    public async Task MixedUserTypeQuery_ShouldDistinguishBetweenStudentsAndTeachers()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Query that should return both students and teachers
        var usersByType = await Context
            .Users.Where(u => u.Status == UserStatus.Active)
            .GroupBy(u => u.Role)
            .Select(g => new
            {
                UserType = g.Key,
                Count = g.Count(),
                Users = g.Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                        TypeSpecificInfo = u is Teacher
                            ? ((Teacher)u).Specialization
                            : ((Student)u).Major,
                    })
                    .ToList(),
            })
            .ToListAsync();

        // Assert
        usersByType.Should().HaveCount(2); // Should have both Student and Teacher groups

        var studentGroup = usersByType.FirstOrDefault(g => g.UserType == UserRole.Student);
        var teacherGroup = usersByType.FirstOrDefault(g => g.UserType == UserRole.Teacher);

        studentGroup.Should().NotBeNull();
        teacherGroup.Should().NotBeNull();

        studentGroup!.Count.Should().BeGreaterThan(0);
        teacherGroup!.Count.Should().BeGreaterThan(0);

        studentGroup.Users.Should().AllSatisfy(u => u.FullName.Should().NotBeNullOrEmpty());
        teacherGroup.Users.Should().AllSatisfy(u => u.FullName.Should().NotBeNullOrEmpty());
    }

    #endregion
}

using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Repositories;

namespace Infrastructure.UnitTests.Repositories;

[TestFixture]
public class UserRepositoryBaseTests : RepositoryTestBase
{
    private UserRepositoryBase<Teacher> _teacherRepository = null!;
    private UserRepositoryBase<Student> _studentRepository = null!;

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();
        _teacherRepository = new UserRepositoryBase<Teacher>(Context, UserManager);
        _studentRepository = new UserRepositoryBase<Student>(Context, UserManager);
    }

    #region Basic CRUD Operations

    [Test]
    public async Task GetByIdAsync_WithValidId_ShouldReturnEntity()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();

        // Act
        var result = await _teacherRepository.GetByIdAsync(teacher.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(teacher.Id);
        result.Email.Should().Be(teacher.Email);
        result.FirstName.Should().Be(teacher.FirstName);
        result.LastName.Should().Be(teacher.LastName);
    }

    [Test]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        await SeedTestDataAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _teacherRepository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetByEmailAsync_WithValidEmail_ShouldReturnEntity()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();

        // Act
        var result = await _teacherRepository.GetByEmailAsync(teacher.Email!);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(teacher.Email);
        result.Id.Should().Be(teacher.Id);
    }

    [Test]
    public async Task GetByEmailAsync_WithInvalidEmail_ShouldReturnNull()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _teacherRepository.GetByEmailAsync("nonexistent@test.com");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetByUserNameAsync_WithValidUserName_ShouldReturnEntity()
    {
        // Arrange
        await SeedTestDataAsync();
        var student = Context.Students.First();

        // Act
        var result = await _studentRepository.GetByUserNameAsync(student.UserName!);

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be(student.UserName);
        result.Id.Should().Be(student.Id);
    }

    [Test]
    public async Task GetByUserNameAsync_WithInvalidUserName_ShouldReturnNull()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _studentRepository.GetByUserNameAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllEntitiesOfType()
    {
        // Arrange
        await SeedTestDataAsync();
        var expectedTeacherCount = Context.Teachers.Count();
        var expectedStudentCount = Context.Students.Count();

        // Act
        var teachers = await _teacherRepository.GetAllAsync();
        var students = await _studentRepository.GetAllAsync();

        // Assert
        teachers.Should().HaveCount(expectedTeacherCount);
        students.Should().HaveCount(expectedStudentCount);

        // Verify TPT inheritance - teachers should only be teachers
        teachers.Should().AllBeOfType<Teacher>();
        students.Should().AllBeOfType<Student>();
    }

    [Test]
    public async Task AddAsync_WithValidEntity_ShouldAddToDatabase()
    {
        // Arrange
        var teacher = CreateTestTeacher(
            "newteacher@test.com",
            "newteacher",
            "New",
            "Teacher",
            "New teacher bio",
            "New Subject"
        );

        // Act
        var result = await _teacherRepository.AddAsync(teacher);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(teacher.Id);

        // Verify in database
        var dbTeacher = await Context.Teachers.FindAsync(teacher.Id);
        dbTeacher.Should().NotBeNull();
        dbTeacher!.Email.Should().Be("newteacher@test.com");
        dbTeacher.FirstName.Should().Be("New");
        dbTeacher.LastName.Should().Be("Teacher");
        dbTeacher.Specialization.Should().Be("New Subject");
    }

    [Test]
    public async Task UpdateAsync_WithValidEntity_ShouldUpdateInDatabase()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();
        var originalSpecialization = teacher.Specialization;

        teacher.UpdateSpecialization("Updated Specialization");

        // Act
        await _teacherRepository.UpdateAsync(teacher);

        // Assert
        var dbTeacher = await Context.Teachers.FindAsync(teacher.Id);
        dbTeacher.Should().NotBeNull();
        dbTeacher!.Specialization.Should().Be("Updated Specialization");
        dbTeacher.Specialization.Should().NotBe(originalSpecialization);
    }

    [Test]
    public async Task DeleteAsync_WithValidEntity_ShouldRemoveFromDatabase()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();
        var teacherId = teacher.Id;

        // Act
        await _teacherRepository.DeleteAsync(teacher);

        // Assert
        var dbTeacher = await Context.Teachers.FindAsync(teacherId);
        dbTeacher.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_WithValidId_ShouldRemoveFromDatabase()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();
        var teacherId = teacher.Id;

        // Act
        await _teacherRepository.DeleteAsync(teacherId);

        // Assert
        var dbTeacher = await Context.Teachers.FindAsync(teacherId);
        dbTeacher.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_WithInvalidId_ShouldThrowException()
    {
        // Arrange
        await SeedTestDataAsync();
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _teacherRepository.DeleteAsync(nonExistentId)
        );
        exception.Message.Should().Contain($"User with ID {nonExistentId} not found");
    }

    #endregion

    #region Validation Methods

    [Test]
    public async Task ExistsAsync_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();

        // Act
        var result = await _teacherRepository.ExistsAsync(teacher.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task ExistsAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        await SeedTestDataAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _teacherRepository.ExistsAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task EmailExistsAsync_WithExistingEmail_ShouldReturnTrue()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();

        // Act
        var result = await _teacherRepository.EmailExistsAsync(teacher.Email!);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task EmailExistsAsync_WithNonExistingEmail_ShouldReturnFalse()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _teacherRepository.EmailExistsAsync("nonexistent@test.com");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task UserNameExistsAsync_WithExistingUserName_ShouldReturnTrue()
    {
        // Arrange
        await SeedTestDataAsync();
        var student = Context.Students.First();

        // Act
        var result = await _studentRepository.UserNameExistsAsync(student.UserName!);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task UserNameExistsAsync_WithNonExistingUserName_ShouldReturnFalse()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _studentRepository.UserNameExistsAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task IsEmailUniqueAsync_WithUniqueEmail_ShouldReturnTrue()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _teacherRepository.IsEmailUniqueAsync("unique@test.com");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task IsEmailUniqueAsync_WithExistingEmail_ShouldReturnFalse()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();

        // Act
        var result = await _teacherRepository.IsEmailUniqueAsync(teacher.Email!);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task IsEmailUniqueAsync_WithExistingEmailButExcludedUser_ShouldReturnTrue()
    {
        // Arrange
        await SeedTestDataAsync();
        var teacher = Context.Teachers.First();

        // Act
        var result = await _teacherRepository.IsEmailUniqueAsync(teacher.Email!, teacher.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task IsUserNameUniqueAsync_WithUniqueUserName_ShouldReturnTrue()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _studentRepository.IsUserNameUniqueAsync("uniqueusername");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task IsUserNameUniqueAsync_WithExistingUserName_ShouldReturnFalse()
    {
        // Arrange
        await SeedTestDataAsync();
        var student = Context.Students.First();

        // Act
        var result = await _studentRepository.IsUserNameUniqueAsync(student.UserName!);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task IsUserNameUniqueAsync_WithExistingUserNameButExcludedUser_ShouldReturnTrue()
    {
        // Arrange
        await SeedTestDataAsync();
        var student = Context.Students.First();

        // Act
        var result = await _studentRepository.IsUserNameUniqueAsync(student.UserName!, student.Id);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Common Filtering Methods

    [Test]
    public async Task GetActiveUsersAsync_ShouldReturnOnlyActiveUsers()
    {
        // Arrange
        await SeedTestDataAsync();
        var expectedActiveTeachers = Context.Teachers.Count(t => t.Status == UserStatus.Active);
        var expectedActiveStudents = Context.Students.Count(s => s.Status == UserStatus.Active);

        // Act
        var activeTeachers = await _teacherRepository.GetActiveUsersAsync();
        var activeStudents = await _studentRepository.GetActiveUsersAsync();

        // Assert
        activeTeachers.Should().HaveCount(expectedActiveTeachers);
        activeStudents.Should().HaveCount(expectedActiveStudents);

        activeTeachers.Should().AllSatisfy(t => t.Status.Should().Be(UserStatus.Active));
        activeStudents.Should().AllSatisfy(s => s.Status.Should().Be(UserStatus.Active));
    }

    [Test]
    public async Task CountActiveUsersAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await SeedTestDataAsync();
        var expectedActiveTeachers = Context.Teachers.Count(t => t.Status == UserStatus.Active);
        var expectedActiveStudents = Context.Students.Count(s => s.Status == UserStatus.Active);

        // Act
        var activeTeacherCount = await _teacherRepository.CountActiveUsersAsync();
        var activeStudentCount = await _studentRepository.CountActiveUsersAsync();

        // Assert
        activeTeacherCount.Should().Be(expectedActiveTeachers);
        activeStudentCount.Should().Be(expectedActiveStudents);
    }

    [Test]
    public async Task CountAllAsync_ShouldReturnTotalCount()
    {
        // Arrange
        await SeedTestDataAsync();
        var expectedTeacherCount = Context.Teachers.Count();
        var expectedStudentCount = Context.Students.Count();

        // Act
        var teacherCount = await _teacherRepository.CountAllAsync();
        var studentCount = await _studentRepository.CountAllAsync();

        // Assert
        teacherCount.Should().Be(expectedTeacherCount);
        studentCount.Should().Be(expectedStudentCount);
    }

    #endregion

    #region TPT Inheritance Verification

    [Test]
    public async Task TPTInheritance_ShouldMaintainPolymorphicBehavior()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Query base type should return all derived types
        var allUsers = await Context.Users.ToListAsync();
        var teachers = await Context.Teachers.ToListAsync();
        var students = await Context.Students.ToListAsync();

        // Assert
        allUsers.Should().HaveCount(teachers.Count + students.Count);

        // Verify polymorphic behavior
        var teacherUsers = allUsers.OfType<Teacher>().ToList();
        var studentUsers = allUsers.OfType<Student>().ToList();

        teacherUsers.Should().HaveCount(teachers.Count);
        studentUsers.Should().HaveCount(students.Count);

        // Verify each teacher has correct properties
        foreach (var teacher in teacherUsers)
        {
            teacher.Role.Should().Be(UserRole.Teacher);
            teacher.FirstName.Should().NotBeNullOrEmpty();
            teacher.LastName.Should().NotBeNullOrEmpty();
            teacher.FullName.Should().Be($"{teacher.FirstName} {teacher.LastName}");
        }

        // Verify each student has correct properties
        foreach (var student in studentUsers)
        {
            student.Role.Should().Be(UserRole.Student);
            student.FirstName.Should().NotBeNullOrEmpty();
            student.LastName.Should().NotBeNullOrEmpty();
            student.FullName.Should().Be($"{student.FirstName} {student.LastName}");
            student.DateOfBirth.Should().BeBefore(DateTime.Today);
        }
    }

    [Test]
    public async Task TPTInheritance_ShouldSupportJoinQueries()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act - Complex query joining base and derived tables
        var activeUsersWithDetails = await Context
            .Users.Where(u => u.Status == UserStatus.Active)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Role,
                u.Status,
                FullName = u is Teacher ? ((Teacher)u).FullName : ((Student)u).FullName,
                TypeSpecificInfo = u is Teacher ? ((Teacher)u).Specialization : ((Student)u).Major,
            })
            .ToListAsync();

        // Assert
        activeUsersWithDetails.Should().NotBeEmpty();
        activeUsersWithDetails
            .Should()
            .AllSatisfy(u =>
            {
                u.Status.Should().Be(UserStatus.Active);
                u.FullName.Should().NotBeNullOrEmpty();
            });

        var teacherDetails = activeUsersWithDetails.Where(u => u.Role == UserRole.Teacher);
        var studentDetails = activeUsersWithDetails.Where(u => u.Role == UserRole.Student);

        teacherDetails.Should().AllSatisfy(t => t.TypeSpecificInfo.Should().NotBeNull());
        // Students might have null major, so we don't assert that
    }

    #endregion
}

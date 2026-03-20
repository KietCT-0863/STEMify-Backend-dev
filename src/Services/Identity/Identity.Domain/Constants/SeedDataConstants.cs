using Identity.Domain.Enums;

namespace Identity.Domain.Constants;


public static class SeedDataConstants
{
    
    public static class DefaultRoles
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string Member = "Member";
        public static IEnumerable<string> All => new[] { Admin, Staff, Member };
    }

    public static class DefaultUsers
    {
        public static readonly UserSeedData Administrator = new(
            Email: "admin@stemify.com",
            Password: "Admin123!",
            Role: "Admin",
            IsSystem: true
        );

        public static readonly UserSeedData DefaultStaff = new(
            Email: "staff@stemify.com",
            Password: "Staff123!",
            Role: "Staff",
            IsSystem: true
        );

        public static IEnumerable<UserSeedData> All =>
            new[] { Administrator, DefaultStaff };
    }

    
    public static class DefaultUsersV2
    {
        public static readonly UserSeedDataV2 Administrator = new(
            Id: "ab6ffd2c-bab9-4f4a-ad46-b34179655b35",
            Email: "admin@stemify.com",
            Password: "Admin123!",
            Role: "Admin",
            IsSystem: true
        );

        public static readonly UserSeedDataV2 DefaultStaff = new(
            Id: "0874973b-6fe5-427f-83e1-703780aa0bd5",
            Email: "staff@stemify.com",
            Password: "Staff123!",
            Role: "Staff",
            IsSystem: true
        );

        public static readonly UserSeedDataV2 DefaultMember1 = new(
            Id: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Email: "member1@stemify.com",
            Password: "Member123!",
            Role: "Member",
            IsSystem: true
        );

        public static IEnumerable<UserSeedDataV2> All =>
            new[]
            {
                Administrator,
                DefaultStaff,
                DefaultMember1
            };
    }

    /// <summary>
    /// Default Groups seed data
    /// </summary>
    public static class DefaultGroups
    {
        public static readonly GroupSeedData Group1 = new(
            OrganizationId: 1,
            Name: "Lớp 5A",
            CreatedByUserId: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Description: "Lớp 5A - Năm học 2024-2025",
            Code: "LOP5A",
            Grade: GroupGrade.Grade5
        );

        public static readonly GroupSeedData Group2 = new(
            OrganizationId: 1,
            Name: "Lớp 4B",
            CreatedByUserId: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Description: "Lớp 4B - Năm học 2024-2025",
            Code: "LOP4B",
            Grade: GroupGrade.Grade4
        );

        public static readonly GroupSeedData Group3 = new(
            OrganizationId: 1,
            Name: "Lớp 3C",
            CreatedByUserId: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Description: "Lớp 3C - Năm học 2024-2025",
            Code: "LOP3C",
            Grade: GroupGrade.Grade3
        );

        public static readonly GroupSeedData Group4 = new(
            OrganizationId: 1,
            Name: "Lớp 5B",
            CreatedByUserId: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Description: "Lớp 5B - Năm học 2024-2025",
            Code: "LOP5B",
            Grade: GroupGrade.Grade5
        );

        public static readonly GroupSeedData Group5 = new(
            OrganizationId: 1,
            Name: "Lớp 4C",
            CreatedByUserId: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Description: "Lớp 4C - Năm học 2024-2025",
            Code: "LOP4C",
            Grade: GroupGrade.Grade4
        );

        public static readonly GroupSeedData Group6 = new(
            OrganizationId: 1,
            Name: "Lớp 2A",
            CreatedByUserId: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Description: "Lớp 2A - Năm học 2024-2025",
            Code: "LOP2A",
            Grade: GroupGrade.Grade2
        );

        public static readonly GroupSeedData Group7 = new(
            OrganizationId: 1,
            Name: "Lớp 1A",
            CreatedByUserId: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Description: "Lớp 1A - Năm học 2024-2025",
            Code: "LOP1A",
            Grade: GroupGrade.Grade1
        );

        public static readonly GroupSeedData Group8 = new(
            OrganizationId: 1,
            Name: "Lớp 5C",
            CreatedByUserId: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Description: "Lớp 5C - Năm học 2024-2025",
            Code: "LOP5C",
            Grade: GroupGrade.Grade5
        );

        public static readonly GroupSeedData Group9 = new(
            OrganizationId: 1,
            Name: "Lớp 4D",
            CreatedByUserId: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Description: "Lớp 4D - Năm học 2024-2025",
            Code: "LOP4D",
            Grade: GroupGrade.Grade4
        );

        public static readonly GroupSeedData Group10 = new(
            OrganizationId: 1,
            Name: "Lớp 3D",
            CreatedByUserId: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Description: "Lớp 3D - Năm học 2024-2025",
            Code: "LOP3D",
            Grade: GroupGrade.Grade3
        );

        public static readonly GroupSeedData Group11 = new(
            OrganizationId: 1,
            Name: "Lớp 2B",
            CreatedByUserId: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Description: "Lớp 2B - Năm học 2024-2025",
            Code: "LOP2B",
            Grade: GroupGrade.Grade2
        );

        public static readonly GroupSeedData Group12 = new(
            OrganizationId: 1,
            Name: "Lớp 1B",
            CreatedByUserId: "0576cb54-72b7-487d-83de-74e3e78c1050",
            Description: "Lớp 1B - Năm học 2024-2025",
            Code: "LOP1B",
            Grade: GroupGrade.Grade1
        );

        public static IEnumerable<GroupSeedData> All =>
            new[] { Group1, Group2, Group3, Group4, Group5, Group6, Group7, Group8, Group9, Group10, Group11, Group12 };
    }

    /// <summary>
    /// Default OrganizationUsers seed data
    /// </summary>
    public static class DefaultOrganizationUsers
    {

        // Students


        public static readonly OrganizationUserSeedData Student9 = new(
            OrganizationId: 1,
            UserId: "a8b4c5d6-7e8f-9a0b-1c2d-3e4f5a6b7c8d",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 5A",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2015, 4, 20), DateTimeKind.Utc),
            StudentMajor: "Vật lý"
        );

        public static readonly OrganizationUserSeedData Student10 = new(
            OrganizationId: 1,
            UserId: "b9c5d6e7-8f9a-0b1c-2d3e-4f5a6b7c8d9e",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 5A",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2015, 8, 12), DateTimeKind.Utc),
            StudentMajor: "Hóa học"
        );

        public static readonly OrganizationUserSeedData Student11 = new(
            OrganizationId: 1,
            UserId: "c0d6e7f8-9a0b-1c2d-3e4f-5a6b7c8d9e0f",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 5B",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2015, 1, 5), DateTimeKind.Utc),
            StudentMajor: "Sinh học"
        );

        public static readonly OrganizationUserSeedData Student12 = new(
            OrganizationId: 1,
            UserId: "d1e7f8a9-0b1c-2d3e-4f5a-6b7c8d9e0f1a",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 5B",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2015, 9, 18), DateTimeKind.Utc),
            StudentMajor: "Môi trường"
        );

        public static readonly OrganizationUserSeedData Student13 = new(
            OrganizationId: 1,
            UserId: "e2f8a9b0-1c2d-3e4f-5a6b-7c8d9e0f1a2b",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 5B",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2015, 12, 25), DateTimeKind.Utc),
            StudentMajor: "Điện tử"
        );

        public static readonly OrganizationUserSeedData Student14 = new(
            OrganizationId: 1,
            UserId: "f3a9b0c1-2d3e-4f5a-6b7c-8d9e0f1a2b3c",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 4B",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2016, 2, 8), DateTimeKind.Utc),
            StudentMajor: "Cơ khí"
        );

        public static readonly OrganizationUserSeedData Student15 = new(
            OrganizationId: 1,
            UserId: "a4b0c1d2-3e4f-5a6b-7c8d-9e0f1a2b3c4d",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 4B",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2016, 6, 15), DateTimeKind.Utc),
            StudentMajor: "Tự động hóa"
        );

        public static readonly OrganizationUserSeedData Student16 = new(
            OrganizationId: 1,
            UserId: "b5c1d2e3-4f5a-6b7c-8d9e-0f1a2b3c4d5e",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 4C",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2016, 10, 22), DateTimeKind.Utc),
            StudentMajor: "Năng lượng"
        );

        public static readonly OrganizationUserSeedData Student17 = new(
            OrganizationId: 1,
            UserId: "c6d2e3f4-5a6b-7c8d-9e0f-1a2b3c4d5e6f",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 4C",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2016, 11, 30), DateTimeKind.Utc),
            StudentMajor: "Vật liệu"
        );

        public static readonly OrganizationUserSeedData Student18 = new(
            OrganizationId: 1,
            UserId: "d7e3f4a5-6b7c-8d9e-0f1a-2b3c4d5e6f7a",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 3C",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2017, 3, 10), DateTimeKind.Utc),
            StudentMajor: "Thiết kế"
        );

        public static readonly OrganizationUserSeedData Student19 = new(
            OrganizationId: 1,
            UserId: "e8f4a5b6-7c8d-9e0f-1a2b-3c4d5e6f7a8b",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 3C",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2017, 7, 20), DateTimeKind.Utc),
            StudentMajor: "Sáng tạo"
        );

        public static readonly OrganizationUserSeedData Student20 = new(
            OrganizationId: 1,
            UserId: "f9a5b6c7-8d9e-0f1a-2b3c-4d5e6f7a8b9c",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 2A",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2018, 1, 15), DateTimeKind.Utc),
            StudentMajor: "Khoa học cơ bản"
        );

        public static readonly OrganizationUserSeedData Student21 = new(
            OrganizationId: 1,
            UserId: "a0b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0d",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 2A",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2018, 5, 28), DateTimeKind.Utc),
            StudentMajor: "Toán ứng dụng"
        );

        public static readonly OrganizationUserSeedData Student22 = new(
            OrganizationId: 1,
            UserId: "b5c1d2e3-4f5a-6b7c-8d9e-0f1a2b3c4d5e",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 1A",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2019, 2, 10), DateTimeKind.Utc),
            StudentMajor: "Khoa học tự nhiên"
        );

        public static readonly OrganizationUserSeedData Student23 = new(
            OrganizationId: 1,
            UserId: "c6d2e3f4-5a6b-7c8d-9e0f-1a2b3c4d5e6f",
            OrganizationRole: OrganizationRole.Student,
            SubscriptionOrderId: 1,
            GroupName: "Lớp 1A",
            Bio: null,
            StudentDateOfBirth: DateTime.SpecifyKind(new DateTime(2019, 8, 5), DateTimeKind.Utc),
            StudentMajor: "Khám phá khoa học"
        );

        public static IEnumerable<OrganizationUserSeedData> All =>
            new[] { Student9, Student10, Student11, Student12, Student13, Student14, Student15,
                    Student16, Student17, Student18, Student19, Student20, Student21, Student22, Student23 };
    }

    /// <summary>
    /// OAuth/OpenIddict default configuration
    /// </summary>
    public static class OAuth
    {
        public static readonly string[] DefaultScopes =
        {
            "openid",
            "profile",
            "email",
            "roles",
            "stemify-api",
        };

        public static readonly ApplicationSeedData[] DefaultApplications =
        {
            new(
                Name: "STEMify Web Client",
                ClientId: "stemify-web",
                Type: "public",
                RedirectUris: new[] { "https://localhost:3000/api/auth/callback/oidc" },
                PostLogoutRedirectUris: new[] { "https://localhost:3000/" }
            ),
            new(
                Name: "STEMify Web Client Production",
                ClientId: "stemify-web-production",
                Type: "public",
                RedirectUris: new[] { "https://www.stemifi.com/api/auth/callback/oidc", "https://robotsteam.com.vn/api/auth/callback/oidc", "https://www.robotsteam.com.vn/api/auth/callback/oidc" },
                PostLogoutRedirectUris: new[] { "https://www.stemifi.com/", "https://robotsteam.com.vn/", "https://www.robotsteam.com.vn/" }
            ),
            new(
                Name: "STEMify Web Client Vercel",
                ClientId: "stemify-web-client",
                Type: "public",
                RedirectUris: new[] { 
                    "https://ste-mify-frontend-dev.vercel.app/api/auth/callback/oidc",
                    "https://localhost:3000/api/auth/callback/oidc",
                    "https://robotsteam.com.vn/api/auth/callback/oidc",
                    "https://www.robotsteam.com.vn/api/auth/callback/oidc"
                },
                PostLogoutRedirectUris: new[] { 
                    "https://ste-mify-frontend-dev.vercel.app/",
                    "https://localhost:3000/",
                    "https://robotsteam.com.vn/",
                    "https://www.robotsteam.com.vn/"
                }
            ),
            new(
                Name: "STEMify Mobile App",
                ClientId: "stemify-mobile",
                Type: "public",
                RedirectUris: new[] { "stemify://auth/callback" },
                PostLogoutRedirectUris: new[] { "stemify://logout" }
            ),
            new(
                Name: "STEMify API Client",
                ClientId: "stemify-api",
                Type: "confidential",
                RedirectUris: new[] { "https://localhost:7002/signin-oidc" },
                PostLogoutRedirectUris: new[] { "https://localhost:7002/signout-oidc" }
            ),
        };
    }
}

/// <summary>
/// Value object for user seed data
/// Immutable and contains validation rules
/// </summary>
/// <param name="Email">User email address</param>
/// <param name="Password">Default password</param>
/// <param name="Role">User role</param>
/// <param name="IsSystem">Whether this is a system-generated user</param>
public record UserSeedData(string Email, string Password, string Role, bool IsSystem = false)
{
    /// <summary>
    /// Validate seed data according to domain rules
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Email)
            && Email.Contains('@')
            && !string.IsNullOrEmpty(Password)
            && Password.Length >= 8
            && !string.IsNullOrEmpty(Role);
    }

    /// <summary>
    /// Get display name for logging
    /// </summary>
    public string DisplayName => $"{Role} ({Email})";
}

/// <summary>
/// Value object for user seed data
/// Immutable and contains validation rules
/// </summary>
/// <param name="Id">User Id</param>
/// <param name="Email">User email address</param>
/// <param name="Password">Default password</param>
/// <param name="Role">User role</param>
/// <param name="IsSystem">Whether this is a system-generated user</param>
public record UserSeedDataV2(string Id, string Email, string Password, string Role, bool IsSystem = false)
{
    /// <summary>
    /// Validate seed data according to domain rules
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Email)
            && Email.Contains('@')
            && !string.IsNullOrEmpty(Password)
            && Password.Length >= 8
            && !string.IsNullOrEmpty(Role);
    }

    /// <summary>
    /// Get display name for logging
    /// </summary>
    public string DisplayName => $"{Role} ({Email})";
}

/// <summary>
/// Value object for OAuth application seed data
/// </summary>
/// <param name="Name">Application name</param>
/// <param name="ClientId">OAuth client ID</param>
/// <param name="Type">Application type (public/confidential)</param>
/// <param name="RedirectUris">Allowed redirect URIs</param>
/// <param name="PostLogoutRedirectUris">Post-logout redirect URIs</param>
public record ApplicationSeedData(
    string Name,
    string ClientId,
    string Type,
    string[] RedirectUris,
    string[] PostLogoutRedirectUris
);

public record GroupSeedData(
    int OrganizationId,
    string Name,
    string CreatedByUserId,
    string? Description = null,
    string? Code = null,
    GroupGrade? Grade = null
);

public record OrganizationUserSeedData(
    int OrganizationId,
    string UserId,
    OrganizationRole OrganizationRole,
    int SubscriptionOrderId,
    string? GroupName = null,
    string? Bio = null,
    DateTime? StudentDateOfBirth = null,
    string? StudentMajor = null,
    string? TeacherSpecialization = null
);

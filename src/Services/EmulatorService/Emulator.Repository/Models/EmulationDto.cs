using Emulator.Repository.Entities;
using Shared.SeedWork;
using Contracts.Abstractions.Paging;

namespace Emulator.Repository.Models;

/// <summary>
/// Request DTO for creating emulation
/// </summary>
public class CreateEmulationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Visibility { get; set; } = "private";
    public EmulationDefinition Definition { get; set; } = new();
    public string? ThumbnailImageBase64 { get; set; }
    public string? ThumbnailFileName { get; set; }
}

/// <summary>
/// Request DTO for updating emulation
/// </summary>
public class UpdateEmulationRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public EmulationDefinition? Definition { get; set; }
    public string? Status { get; set; }
    public string? ThumbnailImageBase64 { get; set; }
    public string? ThumbnailFileName { get; set; }
}

/// <summary>
/// Filter parameters for listing emulations - extends shared PagingRequestParam
/// </summary>
public class EmulationFilterParams : PagingRequestParam
{
    public string? Search { get; set; }
    public string? Difficulty { get; set; }
    public string? Status { get; set; }
    public string? Visibility { get; set; }
    public List<string>? Tags { get; set; }
    public string? CreatedByUserId { get; set; }

    // Compatibility properties
    public int Page => PageNumber;
    public int Limit => PageSize;
    public string? SortBy => OrderBy;
}

/// <summary>
/// Basic emulation DTO
/// </summary>
public class EmulationDto
{
    public string EmulationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public EmulationStatistics Statistics { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Emulation list item DTO
/// </summary>
public class EmulationListDto
{
    public string EmulationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public EmulationStatistics Statistics { get; set; } = new();
    public UserInfo CreatedBy { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Detailed emulation DTO with full definition
/// </summary>
public class EmulationDetailDto : EmulationDto
{
    public EmulationDefinition Definition { get; set; } = new();
    public UserInfo CreatedBy { get; set; } = new();
}

/// <summary>
/// User info DTO
/// </summary>
public class UserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}

/// <summary>
/// Validation result using shared components
/// </summary>
public class ValidationResult
{
    public bool Valid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationError> Warnings { get; set; } = new();
}

/// <summary>
/// Validation error
/// </summary>
public class ValidationError
{
    public string Type { get; set; } = string.Empty; // error, warning
    public string Message { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? ComponentId { get; set; }
}
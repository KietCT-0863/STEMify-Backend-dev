namespace Identity.Application.Common.Models;

/// <summary>
/// Represents the result of a seeding operation
/// </summary>
public class SeedingResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> Messages { get; init; } = new();
    public int ItemsSeeded { get; init; }
    public int ItemsSkipped { get; init; }

    public static SeedingResult Success(
        int itemsSeeded = 0,
        int itemsSkipped = 0,
        List<string>? messages = null
    ) =>
        new()
        {
            IsSuccess = true,
            ItemsSeeded = itemsSeeded,
            ItemsSkipped = itemsSkipped,
            Messages = messages ?? new List<string>(),
        };

    public static SeedingResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };

    public static SeedingResult Failure(Exception exception) =>
        new() { IsSuccess = false, ErrorMessage = exception.Message };
}

/// <summary>
/// Aggregate result for multiple seeding operations
/// </summary>
public class AggregateSeedingResult
{
    public bool IsSuccess => Results.All(r => r.IsSuccess);
    public List<SeedingResult> Results { get; init; } = new();
    public int TotalItemsSeeded => Results.Sum(r => r.ItemsSeeded);
    public int TotalItemsSkipped => Results.Sum(r => r.ItemsSkipped);
    public List<string> AllMessages => Results.SelectMany(r => r.Messages).ToList();
    public List<string> Errors =>
        Results.Where(r => !r.IsSuccess).Select(r => r.ErrorMessage!).ToList();

    public void AddResult(SeedingResult result)
    {
        Results.Add(result);
    }
}

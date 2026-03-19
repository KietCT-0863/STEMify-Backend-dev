using Identity.Application.Dtos.BulkProvisioning;

namespace Identity.Application.Common.Interfaces.Services;

/// <summary>
/// Service for parsing CSV files for bulk user invitation
/// </summary>
public interface ICsvParserService
{
    /// <summary>
    /// Parse CSV stream and validate data
    /// </summary>
    Task<CsvParseResult> ParseBulkInvitationCsvAsync(
        Stream csvStream,
        string allowedEmailDomain,
        CancellationToken cancellationToken = default);
}




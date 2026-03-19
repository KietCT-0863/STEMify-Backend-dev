using CsvHelper;
using CsvHelper.Configuration;
using Identity.Application.Common.Interfaces.Services;
using Identity.Application.Dtos.BulkProvisioning;
using Identity.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Identity.Infrastructure.Services;

public class CsvParserService : ICsvParserService
{
    private readonly ILogger<CsvParserService> _logger;
    private const int MaxRowsPerFile = 10000;

    public CsvParserService(ILogger<CsvParserService> logger)
    {
        _logger = logger;
    }

    public async Task<CsvParseResult> ParseBulkInvitationCsvAsync(
        Stream csvStream,
        string allowedEmailDomain,
        CancellationToken cancellationToken = default)
    {
        var result = new CsvParseResult
        {
            ValidRows = new List<CsvInvitationRow>(),
            Errors = new List<CsvParseError>()
        };

        try
        {
            using var reader = new StreamReader(csvStream);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null,
                MissingFieldFound = null
            };

            using var csv = new CsvReader(reader, config);

            await csv.ReadAsync();
            csv.ReadHeader();

            var rowNumber = 1;
            var validEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (await csv.ReadAsync())
            {
                rowNumber++;

                if (rowNumber > MaxRowsPerFile)
                {
                    result.Errors.Add(new CsvParseError
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = $"Maximum rows limit ({MaxRowsPerFile}) exceeded"
                    });
                    break;
                }

                try
                {
                    var row = ParseRow(csv, rowNumber, allowedEmailDomain);

                    if (row == null)
                        continue;

                    var validationErrors = ValidateRow(row, allowedEmailDomain, validEmails);

                    if (validationErrors.Any())
                    {
                        foreach (var error in validationErrors)
                        {
                            result.Errors.Add(new CsvParseError
                            {
                                RowNumber = rowNumber,
                                FieldName = "Email",
                                RawValue = row.Email,
                                ErrorMessage = error
                            });
                        }
                    }
                    else
                    {
                        result.ValidRows.Add(row);
                        validEmails.Add(row.Email);
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new CsvParseError
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = $"Error parsing row: {ex.Message}"
                    });
                }
            }

            result.TotalRows = rowNumber - 1;

            _logger.LogInformation(
                "CSV parsing completed. Total: {Total}, Valid: {Valid}, Errors: {Errors}",
                result.TotalRows,
                result.ValidRows.Count,
                result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing CSV file");
            result.Errors.Add(new CsvParseError
            {
                RowNumber = 0,
                ErrorMessage = $"Failed to parse CSV file: {ex.Message}"
            });
        }
       result.Success = !result.Errors.Any();

        return result;
    }

    private CsvInvitationRow? ParseRow(CsvReader csv, int rowNumber, string allowedEmailDomain)
    {
        var email = csv.GetField<string>("email")?.Trim();
        var roleStr = csv.GetField<string>("role")?.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(roleStr))
        {
            return null;
        }

        if (!Enum.TryParse<OrganizationRole>(roleStr, true, out var role))
        {
            return null;
        }

        var firstName = csv.GetField<string>("firstName")?.Trim();
        var lastName = csv.GetField<string>("lastName")?.Trim();
        var fullName = csv.GetField<string>("fullName")?.Trim();
        var groupName = csv.GetField<string>("groupName")?.Trim();
        var groupCode = csv.GetField<string>("groupCode")?.Trim();
        var grade = csv.GetField<string>("grade")?.Trim();
        var externalId = csv.GetField<string>("externalId")?.Trim();

        // If FullName is provided but not FirstName/LastName, try to split
        if (!string.IsNullOrEmpty(fullName) && string.IsNullOrEmpty(firstName))
        {
            var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                firstName = parts[0];
                lastName = parts[1];
            }
            else if (parts.Length == 1)
            {
                firstName = parts[0];
            }
        }

        GroupGrade? parsedGrade = null;
        if (!string.IsNullOrWhiteSpace(grade))
        {
            if (int.TryParse(grade, out var gradeValue) &&
                Enum.IsDefined(typeof(GroupGrade), gradeValue))
            {
                parsedGrade = (GroupGrade)gradeValue;
            }
            else
            {
                throw new InvalidOperationException("Grade must be an integer between 1 and 5");
            }
        }

        return new CsvInvitationRow
        {
            Email = email.ToLowerInvariant(),
            Role = role,
            FirstName = firstName,
            LastName = lastName,
            FullName = fullName,
            GroupName = groupName,
            Grade = parsedGrade,
            GroupCode = string.IsNullOrWhiteSpace(groupCode) ? null : groupCode,
            ExternalId = externalId,
            RowNumber = rowNumber
        };
    }

    private List<string> ValidateRow(
        CsvInvitationRow row,
        string allowedEmailDomain,
        HashSet<string> existingEmails)
    {
        var errors = new List<string>();

        // Validate email format
        if (!IsValidEmail(row.Email))
        {
            errors.Add("Invalid email format");
        }

        // Validate email domain
        // if (!row.Email.EndsWith($"@{allowedEmailDomain}", StringComparison.OrdinalIgnoreCase))
        // {
        //     errors.Add($"Email domain must be @{allowedEmailDomain}");
        // }

        // Check duplicate within file
        if (existingEmails.Contains(row.Email))
        {
            errors.Add("Duplicate email in file");
        }

        // Validate role is allowed for organization
        if (row.Role != OrganizationRole.Student
            && row.Role != OrganizationRole.Teacher
            && row.Role != OrganizationRole.OrganizationAdmin)
        {
            errors.Add($"Invalid role '{row.Role}' for organization user. Allowed: Student, Teacher, OrganizationAdmin");
        }

        // Validate name fields if provided
        if (!string.IsNullOrEmpty(row.FirstName) && row.FirstName.Length > 100)
        {
            errors.Add("FirstName exceeds maximum length (100 characters)");
        }

        if (!string.IsNullOrEmpty(row.LastName) && row.LastName.Length > 100)
        {
            errors.Add("LastName exceeds maximum length (100 characters)");
        }

        if (!string.IsNullOrEmpty(row.GroupCode) && row.GroupCode.Length > 50)
        {
            errors.Add("GroupCode exceeds maximum length (50 characters)");
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Basic email validation regex
            var regex = new Regex(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled,
                TimeSpan.FromMilliseconds(250));

            return regex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }
}


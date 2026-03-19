using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when CSV data is invalid or malformed
/// </summary>
public class InvalidCsvDataException : DomainException
{
    public int TotalRowCount { get; }
    public int ValidRowCount { get; }
    public int ErrorCount { get; }
    public List<CsvParseErrorData> Errors { get; }

    public InvalidCsvDataException(string reason)
        : base(
            $"Invalid CSV data: {reason}",
            "INVALID_CSV_DATA"
        )
    {
        TotalRowCount = 0;
        ValidRowCount = 0;
        ErrorCount = 0;
        Errors = new List<CsvParseErrorData>();
    }

    public InvalidCsvDataException(int rowNumber, string reason)
        : base(
            $"Invalid CSV data at row {rowNumber}: {reason}",
            "INVALID_CSV_DATA"
        )
    {
        TotalRowCount = 0;
        ValidRowCount = 0;
        ErrorCount = 1;
        Errors = new List<CsvParseErrorData>
        {
            new CsvParseErrorData
            {
                RowNumber = rowNumber,
                FieldName = "Unknown",
                ErrorMessage = reason,
                RawValue = ""
            }
        };
    }

    public InvalidCsvDataException(
        int totalRowCount,
        int validRowCount,
        List<CsvParseErrorData> errors,
        string summaryMessage)
        : base(summaryMessage, "INVALID_CSV_DATA")
    {
        TotalRowCount = totalRowCount;
        ValidRowCount = validRowCount;
        ErrorCount = errors.Count;
        Errors = errors;
    }
}

public class CsvParseErrorData
{
    public int RowNumber { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string RawValue { get; set; } = string.Empty;
}

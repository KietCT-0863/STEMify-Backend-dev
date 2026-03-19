using System.Text.RegularExpressions;

namespace Identity.Domain.Services;
public static class GroupCodeBuilder
{
    private const int MaxGroupCodeLength = 50;

        public static string BuildFullGroupCode(string? organizationCode, string? groupSegment, int organizationId)
    {
        var orgCode = BuildOrganizationCodePrefix(organizationCode, organizationId);
        var groupCode = SanitizeCodeSegment(groupSegment);
        
        if (string.IsNullOrEmpty(groupCode))
        {
            groupCode = "DEFAULT";
        }

       var maxSegmentLength = Math.Max(1, MaxGroupCodeLength - orgCode.Length - 1);
        if (groupCode.Length > maxSegmentLength)
        {
            groupCode = groupCode[..maxSegmentLength];
        }

        return $"{orgCode}_{groupCode}";
    }

    public static string BuildOrganizationCodePrefix(string? organizationCode, int organizationId)
    {
        var sanitized = SanitizeCodeSegment(organizationCode);
        if (string.IsNullOrEmpty(sanitized))
        {
            sanitized = $"ORG{organizationId}";
        }

        return sanitized;
    }

    public static string SanitizeCodeSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var cleaned = Regex.Replace(trimmed, @"[^A-Za-z0-9_-]", string.Empty);
        return cleaned.ToUpperInvariant();
    }
}


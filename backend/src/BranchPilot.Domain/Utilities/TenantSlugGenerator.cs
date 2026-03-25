using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BranchPilot.Domain.Utilities;

public static partial class TenantSlugGenerator
{
    public static string Generate(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug = MultipleSeparatorsRegex().Replace(NonAlphaNumericRegex().Replace(builder.ToString(), "-"), "-")
            .Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? "tenant" : slug;
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex("-{2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleSeparatorsRegex();
}

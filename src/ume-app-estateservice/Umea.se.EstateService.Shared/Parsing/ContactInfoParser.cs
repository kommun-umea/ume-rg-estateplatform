using System.Text.RegularExpressions;

namespace Umea.se.EstateService.Shared.Parsing;

/// <summary>
/// Lenient parser for free-text contact strings like "090-123456 / anna.johansson@example.com".
/// The format is not guaranteed, so we scan for an email first (unambiguous), then look for
/// a phone-like run in the remainder. Either or both may be null if absent/unrecognizable.
/// </summary>
public static partial class ContactInfoParser
{
    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    // Matches a run that contains at least five digits and may include +, spaces, -, /, (, ).
    [GeneratedRegex(@"(?:\+?\d[\d\s\-/()]*){5,}\d", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();

    public static (string? Phone, string? Email) Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, null);
        }

        string working = raw;
        string? email = null;

        Match emailMatch = EmailRegex().Match(working);
        if (emailMatch.Success)
        {
            email = emailMatch.Value;
            working = working.Remove(emailMatch.Index, emailMatch.Length);
        }

        string? phone = null;
        Match phoneMatch = PhoneRegex().Match(working);
        if (phoneMatch.Success)
        {
            string candidate = phoneMatch.Value.Trim().TrimEnd('-', '/', ' ', '(').TrimStart('-', '/', ' ', ')');
            if (CountDigits(candidate) >= 5)
            {
                phone = candidate;
            }
        }

        return (phone, email);
    }

    private static int CountDigits(string value)
    {
        int count = 0;
        foreach (char c in value)
        {
            if (c >= '0' && c <= '9')
            {
                count++;
            }
        }
        return count;
    }
}

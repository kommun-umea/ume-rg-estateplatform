using System.Globalization;
using System.Text;

namespace Umea.se.EstateService.Logic.Search.Analysis;

internal static class TextNormalizer
{
    public static string Normalize(string input)
    {
        // Lowercase but preserve locale-specific characters such as å, ä, ö
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return input.ToLowerInvariant();
    }

    public static IEnumerable<string> Tokenize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            yield break;
        }

        string normalized = Normalize(input);
        int start = -1;
        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];
            bool isHyphenJoiner = c == '-' && start != -1 && i > start &&
                char.IsLetterOrDigit(normalized[i - 1]) &&
                i + 1 < normalized.Length && char.IsLetterOrDigit(normalized[i + 1]);
            bool isTokenChar = char.IsLetterOrDigit(c) || isHyphenJoiner;
            if (isTokenChar)
            {
                if (start == -1)
                {
                    start = i;
                }
            }
            else
            {
                if (start != -1)
                {
                    yield return normalized[start..i];
                    start = -1;
                }
            }
        }
        if (start != -1)
        {
            yield return normalized[start..];
        }
    }
}

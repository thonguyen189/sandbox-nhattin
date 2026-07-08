using System.Globalization;
using System.Text;
using System.Text.Json;

namespace NhatTinSandbox.Api.Json;

/// <summary>
/// A net6.0 port of the built-in <c>JsonNamingPolicy.SnakeCaseLower</c> (which only exists in
/// .NET 8+). It reproduces the word-splitting logic of System.Text.Json's internal
/// <c>JsonSeparatorNamingPolicy</c> configured with <c>lowercase = true</c> and
/// <c>separator = '_'</c>, so the sandbox can target net6.0 while producing byte-for-byte
/// identical snake_case output (e.g. <c>SName</c> -&gt; <c>s_name</c>, <c>IOStream</c> -&gt;
/// <c>io_stream</c>).
/// </summary>
public sealed class SnakeCaseLowerNamingPolicy : JsonNamingPolicy
{
    /// <summary>Shared singleton, drop-in replacement for <c>JsonNamingPolicy.SnakeCaseLower</c>.</summary>
    public static SnakeCaseLowerNamingPolicy Instance { get; } = new();

    private const char Separator = '_';

    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 8);

        int first = 0;
        ReadOnlySpan<char> chars = name.AsSpan();
        CharCategory previousCategory = CharCategory.Boundary;

        for (int index = 0; index < chars.Length; index++)
        {
            char current = chars[index];
            UnicodeCategory currentCategoryUnicode = char.GetUnicodeCategory(current);

            if (currentCategoryUnicode == UnicodeCategory.SpaceSeparator ||
                (currentCategoryUnicode >= UnicodeCategory.ConnectorPunctuation &&
                 currentCategoryUnicode <= UnicodeCategory.OtherPunctuation))
            {
                WriteWord(chars.Slice(first, index - first), builder);

                previousCategory = CharCategory.Boundary;
                first = index + 1;

                continue;
            }

            if (index + 1 < chars.Length)
            {
                char next = chars[index + 1];
                CharCategory currentCategory = currentCategoryUnicode switch
                {
                    UnicodeCategory.LowercaseLetter => CharCategory.Lowercase,
                    UnicodeCategory.UppercaseLetter => CharCategory.Uppercase,
                    _ => previousCategory,
                };

                if ((currentCategory == CharCategory.Lowercase && char.IsUpper(next)) || next == '_')
                {
                    WriteWord(chars.Slice(first, index - first + 1), builder);

                    previousCategory = CharCategory.Boundary;
                    first = index + 1;
                }
                else if (previousCategory == CharCategory.Uppercase &&
                         currentCategoryUnicode == UnicodeCategory.UppercaseLetter &&
                         char.IsLower(next))
                {
                    WriteWord(chars.Slice(first, index - first), builder);

                    previousCategory = CharCategory.Boundary;
                    first = index;
                }
                else
                {
                    previousCategory = currentCategory;
                }
            }
        }

        WriteWord(chars.Slice(first), builder);

        return builder.ToString();
    }

    private static void WriteWord(ReadOnlySpan<char> word, StringBuilder builder)
    {
        if (word.IsEmpty)
        {
            return;
        }

        if (builder.Length != 0)
        {
            builder.Append(Separator);
        }

        foreach (char c in word)
        {
            builder.Append(char.ToLowerInvariant(c));
        }
    }

    private enum CharCategory
    {
        Boundary,
        Lowercase,
        Uppercase,
    }
}

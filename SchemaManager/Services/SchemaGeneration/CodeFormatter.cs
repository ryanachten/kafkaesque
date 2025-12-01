using System.Text;
using System.Text.RegularExpressions;

namespace SchemaManager.Services.SchemaGeneration;

/// <summary>
/// Formats generated Avro code to convert property names to PascalCase.
/// </summary>
public static class CodeFormatter
{
    /// <summary>
    /// Converts property names in generated C# code from camelCase to PascalCase.
    /// </summary>
    public static string ConvertToPascalCase(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return code;

        var result = new StringBuilder(code);
        var originalCode = code;

        // Pattern 1: Property declarations: "public Type propertyName {"
        // This matches properties that start with lowercase
        var propertyPattern = new Regex(
            @"(?m)^(\s*)public\s+([A-Za-z0-9_.<>\[\]\s\?]+)\s+([a-z][a-zA-Z0-9_]*)\s*\{",
            RegexOptions.Multiline);

        var propertyMatches = propertyPattern.Matches(code);
        var replacements = new List<(int start, int length, string replacement)>();

        foreach (Match match in propertyMatches)
        {
            var propName = match.Groups[3].Value;
            var pascalName = ToPascalCase(propName);
            if (propName != pascalName)
            {
                replacements.Add((match.Groups[3].Index, match.Groups[3].Length, pascalName));
            }
        }

        // Pattern 2: Field declarations: "private Type _fieldName;"
        var fieldPattern = new Regex(
            @"(?m)^(\s*)private\s+([A-Za-z0-9_.<>\[\]\s\?]+)\s+_([a-z][a-zA-Z0-9_]*)\s*;",
            RegexOptions.Multiline);

        var fieldMatches = fieldPattern.Matches(code);
        foreach (Match match in fieldMatches)
        {
            var fieldName = match.Groups[3].Value;
            var pascalName = ToPascalCase(fieldName);
            if (fieldName != pascalName)
            {
                replacements.Add((match.Groups[3].Index, match.Groups[3].Length, pascalName));
            }
        }

        // Pattern 3: Property references: "this.propertyName" or "this._fieldName"
        var thisRefPattern = new Regex(
            @"\bthis\.(_?)([a-z][a-zA-Z0-9_]*)",
            RegexOptions.Multiline);

        var thisRefMatches = thisRefPattern.Matches(code);
        foreach (Match match in thisRefMatches)
        {
            var propName = match.Groups[2].Value;
            var pascalName = ToPascalCase(propName);
            if (propName != pascalName)
            {
                replacements.Add((match.Groups[2].Index, match.Groups[2].Length, pascalName));
            }
        }

        // Apply replacements in reverse order to maintain indices
        replacements.Sort((a, b) => b.start.CompareTo(a.start));
        foreach (var (start, length, replacement) in replacements)
        {
            result.Remove(start, length);
            result.Insert(start, replacement);
        }

        return result.ToString();
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpperInvariant(input[0]) + input.Substring(1);
    }
}


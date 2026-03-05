namespace GitDesktop.App.ViewModels;

public enum SourceLanguage
{
    Unknown,
    CSharp,
    Java,
    JavaScript,
    TypeScript,
    Python,
    Ruby,
    Shell,
    Sql,
    Css,
    Html,
    Json,
    Xml,
    Yaml,
    Markdown,
}

public static class SourceSyntaxClassifier
{
    public static SourceLanguage DetectLanguage(string? filePath)
    {
        var ext = Path.GetExtension(filePath ?? string.Empty).ToLowerInvariant();
        return ext switch
        {
            ".cs" => SourceLanguage.CSharp,
            ".java" => SourceLanguage.Java,
            ".js" or ".jsx" or ".mjs" => SourceLanguage.JavaScript,
            ".ts" or ".tsx" => SourceLanguage.TypeScript,
            ".py" => SourceLanguage.Python,
            ".rb" => SourceLanguage.Ruby,
            ".sh" or ".bash" or ".zsh" => SourceLanguage.Shell,
            ".sql" => SourceLanguage.Sql,
            ".css" => SourceLanguage.Css,
            ".html" or ".htm" => SourceLanguage.Html,
            ".json" => SourceLanguage.Json,
            ".xml" => SourceLanguage.Xml,
            ".yaml" or ".yml" => SourceLanguage.Yaml,
            ".md" => SourceLanguage.Markdown,
            _ => SourceLanguage.Unknown,
        };
    }

    public static FileLineKind ClassifyLine(string line, SourceLanguage language)
    {
        var trimmed = line.TrimStart();
        if (string.IsNullOrEmpty(trimmed))
            return FileLineKind.Code;

        if (IsComment(trimmed, language))
            return FileLineKind.Comment;

        if (IsString(trimmed))
            return FileLineKind.String;

        if (StartsWithKeyword(trimmed, language))
            return FileLineKind.Keyword;

        return FileLineKind.Code;
    }

    private static bool IsComment(string trimmed, SourceLanguage language) =>
        language switch
        {
            SourceLanguage.Python or SourceLanguage.Ruby or SourceLanguage.Shell or SourceLanguage.Yaml
                => trimmed.StartsWith("#"),
            SourceLanguage.Sql => trimmed.StartsWith("--"),
            SourceLanguage.Html or SourceLanguage.Xml => trimmed.StartsWith("<!--"),
            SourceLanguage.Json => false,
            _ => trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*"),
        };

    private static bool IsString(string trimmed) =>
        HasLikelyStringLiteral(trimmed) &&
        (trimmed.StartsWith("\"") || trimmed.StartsWith("'") ||
         trimmed.Contains(" = \"", StringComparison.Ordinal) ||
         trimmed.Contains(" = '", StringComparison.Ordinal) ||
         trimmed.Contains(": \"", StringComparison.Ordinal));

    private static bool HasLikelyStringLiteral(string trimmed)
    {
        var dbl = trimmed.Count(c => c == '"');
        var sgl = trimmed.Count(c => c == '\'');
        return dbl >= 2 || sgl >= 2 || trimmed.StartsWith("\"") || trimmed.StartsWith("'");
    }

    private static bool StartsWithKeyword(string trimmed, SourceLanguage language)
    {
        foreach (var keyword in Keywords(language))
        {
            if (trimmed.StartsWith(keyword + " ", StringComparison.Ordinal) ||
                trimmed.StartsWith(keyword + "\t", StringComparison.Ordinal) ||
                trimmed == keyword)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> Keywords(SourceLanguage language) =>
        language switch
        {
            SourceLanguage.CSharp => ["namespace", "using", "class", "interface", "struct", "enum", "public", "private", "protected", "internal", "static", "async", "await", "return", "if", "else", "for", "foreach", "while", "switch", "new", "var"],
            SourceLanguage.Java => ["package", "import", "class", "interface", "enum", "public", "private", "protected", "static", "final", "return", "if", "else", "for", "while", "switch", "new"],
            SourceLanguage.JavaScript or SourceLanguage.TypeScript => ["import", "export", "class", "function", "const", "let", "var", "async", "await", "return", "if", "else", "for", "while", "switch", "new"],
            SourceLanguage.Python => ["def", "class", "import", "from", "return", "if", "elif", "else", "for", "while", "try", "except", "async", "await"],
            SourceLanguage.Ruby => ["class", "module", "def", "if", "elsif", "else", "end", "require", "return"],
            SourceLanguage.Shell => ["if", "then", "else", "fi", "for", "while", "do", "done", "case", "esac", "function"],
            SourceLanguage.Sql => ["select", "insert", "update", "delete", "from", "where", "join", "create", "alter", "drop", "into", "values"],
            SourceLanguage.Css => ["@media", "@import"],
            SourceLanguage.Html or SourceLanguage.Xml => ["<!doctype", "<html", "<head", "<body", "<script", "<style"],
            SourceLanguage.Json => ["true", "false", "null"],
            SourceLanguage.Yaml => ["true", "false", "null"],
            _ => [],
        };
}

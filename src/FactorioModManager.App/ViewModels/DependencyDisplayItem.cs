namespace FactorioModManager.App.ViewModels;

public sealed record DependencyDisplayItem(string Text, string Foreground)
{
    public static IReadOnlyList<DependencyDisplayItem> ParseAll(IReadOnlyList<string> rawDeps)
    {
        return rawDeps.Select(Parse).ToList();
    }

    private static DependencyDisplayItem Parse(string raw)
    {
        var s = raw.Trim();

        if (s.StartsWith("(?)"))
            return new($"◦ {s[3..].TrimStart()}", "#6A604F");

        if (s.StartsWith('!'))
            return new($"✕ {s[1..].TrimStart()}", "#E89089");

        if (s.StartsWith('?'))
            return new($"◦ {s[1..].TrimStart()}", "#8A7D65");

        if (s.StartsWith('~'))
            return new($"~ {s[1..].TrimStart()}", "#6A604F");

        return new($"• {s}", "#A89C84");
    }
}

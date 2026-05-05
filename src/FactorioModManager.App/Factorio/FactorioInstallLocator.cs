using System.Text.RegularExpressions;

namespace FactorioModManager.App.Factorio;

public sealed class FactorioInstallLocator
{
    private static readonly Regex SteamLibraryPathPattern = new(
        "\"path\"\\s+\"(?<path>.+?)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IReadOnlyList<string>? _candidateInstallFolders;
    private readonly IReadOnlyList<string>? _steamLibraryFiles;

    public FactorioInstallLocator(
        IEnumerable<string>? candidateInstallFolders = null,
        IEnumerable<string>? steamLibraryFiles = null)
    {
        _candidateInstallFolders = candidateInstallFolders?.ToList();
        _steamLibraryFiles = steamLibraryFiles?.ToList();
    }

    public string? Locate(string? preferredInstallFolderPath, string? modsFolderPath)
    {
        if (IsInstallFolder(preferredInstallFolderPath))
        {
            return NormalizePath(preferredInstallFolderPath!);
        }

        foreach (var candidate in EnumerateCandidateInstallFolders(modsFolderPath).Distinct(PathComparer.Instance))
        {
            if (IsInstallFolder(candidate))
            {
                return NormalizePath(candidate);
            }
        }

        return null;
    }

    public bool IsInstallFolder(string? folderPath)
    {
        return !string.IsNullOrWhiteSpace(folderPath) &&
            Directory.Exists(Path.Combine(folderPath, "data"));
    }

    private IEnumerable<string> EnumerateCandidateInstallFolders(string? modsFolderPath)
    {
        if (!string.IsNullOrWhiteSpace(modsFolderPath))
        {
            var modsParent = Directory.GetParent(modsFolderPath);
            if (modsParent is not null)
            {
                yield return modsParent.FullName;
            }
        }

        foreach (var candidate in _candidateInstallFolders ?? EnumerateDefaultCandidateInstallFolders())
        {
            yield return candidate;
        }

        foreach (var steamLibraryRoot in EnumerateSteamLibraryRoots())
        {
            yield return Path.Combine(steamLibraryRoot, "steamapps", "common", "Factorio");
        }
    }

    private IEnumerable<string> EnumerateDefaultCandidateInstallFolders()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        foreach (var root in new[] { programFiles, programFilesX86 })
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            yield return Path.Combine(root, "Factorio");
            yield return Path.Combine(root, "Steam", "steamapps", "common", "Factorio");
        }

        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            yield return Path.Combine(localAppData, "Programs", "Factorio");
        }
    }

    private IEnumerable<string> EnumerateSteamLibraryRoots()
    {
        foreach (var libraryFile in _steamLibraryFiles ?? EnumerateDefaultSteamLibraryFiles())
        {
            if (!File.Exists(libraryFile))
            {
                continue;
            }

            IEnumerable<string> lines;
            try
            {
                lines = File.ReadLines(libraryFile).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var line in lines)
            {
                var match = SteamLibraryPathPattern.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                var path = match.Groups["path"].Value.Replace(@"\\", @"\");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    yield return path;
                }
            }
        }
    }

    private IEnumerable<string> EnumerateDefaultSteamLibraryFiles()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        foreach (var root in new[] { programFiles, programFilesX86 })
        {
            if (!string.IsNullOrWhiteSpace(root))
            {
                yield return Path.Combine(root, "Steam", "steamapps", "libraryfolders.vdf");
            }
        }
    }

    private static string NormalizePath(string folderPath)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(folderPath));
    }

    private sealed class PathComparer : IEqualityComparer<string>
    {
        public static readonly PathComparer Instance = new();

        public bool Equals(string? x, string? y)
        {
            if (string.IsNullOrWhiteSpace(x) || string.IsNullOrWhiteSpace(y))
            {
                return false;
            }

            try
            {
                return string.Equals(NormalizePath(x), NormalizePath(y), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
            }
        }

        public int GetHashCode(string obj)
        {
            try
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(NormalizePath(obj));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
            }
        }
    }
}

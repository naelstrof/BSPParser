namespace BSPParser;

public static class CaseSensitivityTools {
    private static bool FileExistsCaseSensitive(string filename) {
        string? name = Path.GetDirectoryName(filename);
        return name != null && Array.Exists(Directory.GetFiles(name), s => s == Path.GetFullPath(filename));
    }

    public static void FixMalformedCasing(ICollection<string> correctPaths) {
        foreach (var path in correctPaths) {
            var fileName = Path.GetFileName(path);
            var directory = Path.GetDirectoryName(path);
            if (directory == null) {
                continue;
            }

            var directoryInfo = new DirectoryInfo(directory);
            if (!directoryInfo.Exists) {
                continue;
            }

            if (FileExistsCaseSensitive(Path.Combine(directoryInfo.FullName, fileName))) {
                continue;
            }

            foreach (var file in directoryInfo.GetFiles()) {
                if (file.Name.ToLowerInvariant() == fileName.ToLowerInvariant() && file.Name != fileName) {
                    Console.WriteLine($"Renaming {file.FullName} to {Path.Combine(directoryInfo.FullName, fileName)}");
                    File.Move(file.FullName, Path.Combine(directoryInfo.FullName, fileName));
                }
            }
        }
    }
    
}
namespace BSPParser;

public static class PathExtensions {
    public static string GetFileName(string str) {
        str = str.Replace('\\', '/');
        return Path.GetFileName(str);
    }
    public static string GetExtension(string str) {
        str = str.Replace('\\', '/');
        return Path.GetExtension(str);
    }
    public static string GetFileNameWithoutExtension(string str) {
        str = str.Replace('\\', '/');
        return Path.GetFileNameWithoutExtension(str);
    }
    public static string? GetDirectoryName(string str) {
        str = str.Replace('\\', '/');
        return Path.GetDirectoryName(str);
    }

    public static string Combine(string pathA, string pathB) {
        pathA = pathA.Replace('\\', '/');
        pathB = pathB.Replace('\\', '/');
        string output = Path.Combine(pathA, pathB);
        output = output.Replace('\\', '/');
        return output;
    }
    
    public static string Combine(string pathA, string pathB, string pathC) {
        pathA = pathA.Replace('\\', '/');
        pathB = pathB.Replace('\\', '/');
        pathC = pathC.Replace('\\', '/');
        string output = Path.Combine(pathA, pathB, pathC);
        output = output.Replace('\\', '/');
        return output;
    }
}
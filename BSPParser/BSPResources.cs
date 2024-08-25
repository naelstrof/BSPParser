namespace BSPParser;

public class BSPResources : Dictionary<string,BSPResource> {
    public void Clean() {
        var removePairs = this.Where((pair) => string.IsNullOrEmpty(pair.Key.Trim()) || pair.Key.Trim().StartsWith("//"));
        foreach (var pair in removePairs) {
            Remove(pair.Key);
        }
    }

    public void LoadFromResourceFile(string path) {
        var filesource = new BSPResourceFileSource(path);
        foreach (var line in File.ReadLines(path)) {
            Add(line.Trim(), new BSPResource(line.Trim(),filesource));
        }
    }
}
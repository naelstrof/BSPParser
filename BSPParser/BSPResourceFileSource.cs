namespace BSPParser;

public class BSPResourceFileSource : IResourceSource {
    private string filepath;
    public BSPResourceFileSource(string filepath) {
        this.filepath = filepath;
    }

    public string GetResourceDescription() {
        return $"[File:{filepath}]";
    }
}
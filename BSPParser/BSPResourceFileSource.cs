namespace BSPParser;

public class BSPResourceFileSource : IResourceSource {
    private string filepath;
    public BSPResourceFileSource(string filepath) {
        this.filepath = filepath;
    }

    public override string ToString() {
        return GetResourceDescription();
    }

    public string GetResourceDescription() {
        return $"[File:{Path.GetFileName(filepath)}]";
    }
}
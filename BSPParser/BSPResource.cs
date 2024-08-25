namespace BSPParser;

public struct BSPResource {
    public string filePath;
    public IResourceSource source;

    public BSPResource(string path, IResourceSource source) {
        this.filePath = path;
        this.source = source;
    }

    public override string ToString() {
        return $"{filePath}: {source.ToString()}";
    }
}
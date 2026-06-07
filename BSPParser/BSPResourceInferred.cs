namespace BSPParser;

public class BSPResourceInferred(string source) : IResourceSource {
    private string source = source;
    public override string ToString() {
        return GetResourceDescription();
    }

    public string GetResourceDescription() {
        return $"[Added by inferring: {source}]";
    }
}
namespace BSPParser;

public class BSPResourceInferred(string source) : IResourceSource {
    private string source = source;
    public override string ToString() {
        return GetResourceDescription();
    }

    public bool GetInferred() => true;

    public string GetResourceDescription() {
        return $"[Added by inferring: {source}]";
    }
}
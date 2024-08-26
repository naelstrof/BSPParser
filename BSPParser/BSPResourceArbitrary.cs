namespace BSPParser;

public class BSPResourceArbitrary(string source) : IResourceSource {
    private string source = source;
    public override string ToString() {
        return GetResourceDescription();
    }

    public string GetResourceDescription() {
        return $"[Added arbitrarily: {source}]";
    }
}
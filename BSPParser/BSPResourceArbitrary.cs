namespace BSPParser;

public class BSPResourceArbitrary(string source) : IResourceSource {
    private string source = source;
    public override string ToString() {
        return GetResourceDescription();
    }
    public bool GetInferred() => false;

    public string GetResourceDescription() {
        return $"[Added arbitrarily: {source}]";
    }
}
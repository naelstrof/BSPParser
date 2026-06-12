namespace BSPParser;

public class BSPResourceEntitySource : IResourceSource {
    private BSPEntity entity;

    public bool GetInferred() => false;
    public BSPResourceEntitySource(BSPEntity entity) {
        this.entity = entity;
    }
    public override string ToString() => GetResourceDescription();
    public string GetResourceDescription() {
        return $"[Entity:{entity["classname"]}, in: {entity.GetParent()}]";
    }
}
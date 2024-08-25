using System.Text;

namespace BSPParser;

public class BSPEntity : Dictionary<string,string>, IResourceSource {
    private BSP parent;
    public BSPEntity(BSP parent) : base() {
        this.parent = parent;
    }
    public override string ToString() {
        StringBuilder builder = new StringBuilder();
        foreach (var pair in this) {
            builder.Append($"\"{pair.Key}\" \"{pair.Value}\"\n");
        }
        return builder.ToString();
    }

    public string GetResourceDescription() {
        return $"[Entity:{this["classname"]}, in: {parent}]";
    }
}
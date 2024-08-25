using System.Text;

namespace BSPParser;

public class BSPEntity : Dictionary<string,string> {
    public override string ToString() {
        StringBuilder builder = new StringBuilder();
        foreach (var pair in this) {
            builder.Append($"\"{pair.Key}\" \"{pair.Value}\"\n");
        }
        return builder.ToString();
    }
}
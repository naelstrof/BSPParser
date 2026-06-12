using System.Text;

namespace BSPParser;

public class WeaponHudTokenizer(string tokens) {
    private int ptr = 0;
    public IEnumerable<string> GetAllSprites() {
        List<string> buffer = new List<string>();
        while (ptr < tokens.Length) {
            Trim();
            if (ptr >= tokens.Length || tokens[ptr] == '\n') {
                if (buffer.Count > 2) {
                    yield return buffer[2];
                }
                buffer.Clear();
                ptr++;
            }
            if (TryParseKey(out string key)) {
                buffer.Add(key);
            }
        }
        if (buffer.Count > 2) {
            yield return buffer[2];
        }
        buffer.Clear();
    }
    
    private void Trim() {
        while (ptr < tokens.Length && (char.IsWhiteSpace(tokens[ptr]) && tokens[ptr] != '\n')) { ptr++; }
        // Skip comments
        while (ptr < tokens.Length && tokens[ptr] == '#') {
            while (ptr < tokens.Length && tokens[ptr] != '\n') { ptr++; }
            Trim();
        }
    }

    private bool TryParseKey(out string key) {
        if (ptr >= tokens.Length) {
            key = "";
            return false;
        }
        StringBuilder builder = new StringBuilder();
        if (tokens[ptr] == '"') {
            return TryParseString(out key);
        }
        
        while (ptr < tokens.Length && !char.IsWhiteSpace(tokens[ptr])) {
            builder.Append(tokens[ptr++]);
        }
        
        key = builder.ToString();
        return builder.Length != 0;
    }
    
    private bool TryParseString(out string str) {
        if (ptr >= tokens.Length || tokens[ptr++] != '"') {
            str = "";
            return false;
        }
        StringBuilder builder = new StringBuilder();
        while (ptr < tokens.Length) {
            if (tokens[ptr] == '"' && (ptr < 2 || (tokens[ptr - 1] != '\\' || tokens[ptr-2] == '\\'))) {
                ptr++;
                str = builder.ToString();
                return true;
            }
            builder.Append(tokens[ptr++]);
        }
        str = "";
        return false;
    }
}
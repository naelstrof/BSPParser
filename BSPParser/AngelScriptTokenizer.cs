using System.Text;
namespace BSPParser;

public class AngelScriptTokenizer(string tokens) {
    private int ptr = 0;
    private void Trim() {
        while (ptr < tokens.Length && char.IsWhiteSpace(tokens[ptr])) { ptr++; }

        // Skip multi-block comments
        if (ptr < tokens.Length - 1 && tokens[ptr] == '/' && tokens[ptr + 1] == '*') {
            while (ptr < tokens.Length && !(tokens[ptr] == '*' && tokens[ptr + 1] == '/')) {
                ptr++;
            }
        }

        // Skip comments
        while (ptr < tokens.Length-1 && tokens[ptr] == '/' && tokens[ptr + 1] == '/') {
            while (ptr < tokens.Length && tokens[ptr] != '\n') { ptr++; }
            Trim();
        }
    }

    private bool TryParseKeyword(string keyword) {
        int resetPtr = ptr;
        for (int i = 0; ptr < tokens.Length && i < keyword.Length; i++) {
            if (tokens[ptr] != keyword[i]) {
                ptr = resetPtr;
                return false;
            }
            ptr++;
        }
        return true;
    }
    
    private bool TryParseInclude(out string include) {
        if (!TryParseKeyword("#include")) {
            include = "";
            return false;
        }
        Trim();
        return TryParseString(out include);
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

    public IEnumerable<string> GetAllStrings() {
        ptr = 0;
        while (ptr < tokens.Length) {
            Trim();
            if (TryParseString(out var str)) {
                yield return str;
            } else {
                ptr++; // Just skip tokens we don't understand...
            }
        }
    }

    public IEnumerable<string> GetAllIncludes() {
        ptr = 0;
        while (ptr < tokens.Length) {
            Trim();
            if (TryParseInclude(out var include)) {
                yield return include;
            } else {
                ptr++; // Just skip tokens we don't understand...
            }
        }
    }
}
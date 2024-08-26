using System.Collections;
using System.Diagnostics;
using System.Text;

namespace BSPParser;

public class BSPTokenizer(string tokens, BSP? bsp = null) : IEnumerable<BSPEntity> {
    private int ptr = 0;
    private void Trim() {
        while (ptr < tokens.Length && char.IsWhiteSpace(tokens[ptr])) { ptr++; }
        // Skip comments
        while (ptr < tokens.Length-1 && tokens[ptr] == '/' && tokens[ptr + 1] == '/') {
            while (ptr < tokens.Length && tokens[ptr] != '\n') { ptr++; }
            Trim();
        }
    }
    private bool TryParseString(out string str) {
        if (ptr >= tokens.Length || tokens[ptr++] != '"') {
            str = "";
            return false;
        }
        StringBuilder builder = new StringBuilder();
        while (ptr < tokens.Length) {
            if (tokens[ptr] == '"' && tokens[ptr - 1] != '\\') {
                ptr++;
                str = builder.ToString();
                return true;
            }
            builder.Append(tokens[ptr++]);
        }
        str = "";
        return false;
    }

    private bool TryParseKeyValue(out string key, out string value) {
        Trim();
        if (!TryParseString(out key)) {
            value = "";
            return false;
        }
        Trim();
        if (!TryParseString(out value)) {
            return false;
        }
        Trim();
        return true;
    }

    private bool TryGetNextEntity(out BSPEntity entity) {
        entity = new BSPEntity(bsp);
        Trim();
        if (tokens[ptr++] != '{') {
            return false;
        }
        Trim();
        while (ptr < tokens.Length && tokens[ptr] != '}') {
            if (TryParseKeyValue(out var key, out var value)) {
                entity.TryAdd(key, value);
            }
        }
        Trim();
        if (tokens[ptr++] != '}') {
            return false;
        }
        return true;
    }

    public IEnumerable<KeyValuePair<string, string>> GetKeyValues() {
        while (TryParseKeyValue(out string key, out string value)) {
            yield return new KeyValuePair<string, string>(key, value);
        }
    }

    public IEnumerator<BSPEntity> GetEnumerator() {
        ptr = 0;
        while (TryGetNextEntity(out BSPEntity entity)) {
            yield return entity;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}
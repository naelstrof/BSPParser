using System.Collections;
using System.Diagnostics;
using System.Text;

namespace BSPParser;

public class BSPEntityTokenizer(string tokens) : IEnumerable<BSPEntity> {
    private int ptr = 0;
    private void Trim() {
        while (ptr < tokens.Length && char.IsWhiteSpace(tokens[ptr])) { ptr++; }
    }
    
    private string ParseString() {
        int start = ptr;
        if (tokens[ptr++] != '"') {
            throw new Exception($"strings must start with a double quote... started with {tokens[ptr-1]} instead");
        }
        StringBuilder builder = new StringBuilder();
        while (ptr < tokens.Length) {
            if (tokens[ptr] == '"' && tokens[ptr - 1] != '\\') {
                ptr++;
                return builder.ToString();
            }
            builder.Append(tokens[ptr++]);
        }
        throw new Exception($"string {tokens.Substring(start, int.Min(tokens.Length-start,16))} didn't end with a double quote..");
    }

    private bool TryGetNextEntity(out BSPEntity entity) {
        entity = new BSPEntity();
        Trim();
        if (tokens[ptr++] != '{') {
            return false;
        }
        Trim();
        while (ptr < tokens.Length && tokens[ptr] != '}') {
            Trim();
            var key = ParseString();
            Trim();
            var value = ParseString();
            entity.TryAdd(key, value);
            Trim();
        }
        Trim();
        if (tokens[ptr++] != '}') {
            return false;
        }
        return true;
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
using System.Text;

namespace BSPParser;

public class SvenConfig : Dictionary<string,string> {
    private int ptr = 0;
    private string tokens;
    
    public SvenConfig(string tokens) {
        this.tokens = tokens;
        while (TryParseKeyValue(out var key, out var value)) {
            TryAdd(key, value);
        }
    }
    private void Trim() {
        while (ptr < tokens.Length && char.IsWhiteSpace(tokens[ptr])) { ptr++; }
        // Skip comments
        while (ptr < tokens.Length-1 && tokens[ptr] == '#') {
            while (ptr < tokens.Length && tokens[ptr] != '\n') { ptr++; }
            Trim();
        }
    }
    
    private bool TryParseString(out string str) {
        StringBuilder builder = new StringBuilder();
        bool seenQuote = false;
        while (ptr < tokens.Length && (!char.IsWhiteSpace(tokens[ptr]) || (seenQuote && tokens[ptr] != '\n'))) {
            if (tokens[ptr] == '"' && ptr != 0 && tokens[ptr - 1] != '\\') {
                seenQuote = !seenQuote;
            }
            builder.Append(tokens[ptr++]);
        }
        
        ptr++;

        if (seenQuote) {
            str = builder.ToString();
            Console.WriteLine("Failed to find end quote to string in config...");
            return false;
        }

        if (builder.Length > 0) {
            str = builder.ToString();
            return true;
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
}
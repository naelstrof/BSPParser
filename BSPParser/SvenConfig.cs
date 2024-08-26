using System.Text;

namespace BSPParser;

public class SvenConfig : Dictionary<string,string> {
    private int ptr = 0;
    private string tokens;
    
    public SvenConfig(string tokens) {
        this.tokens = tokens;
        List<string> buffer = new List<string>();
        while (ptr < this.tokens.Length) {
            buffer.Clear();
            while (!Trim() && TryParseString(out var token)) {
                buffer.Add(token);
            }

            if (buffer.Count == 1) {
                TryAdd(buffer[0], "");
            } else if (buffer.Count == 2) {
                TryAdd(buffer[0], buffer[1].Trim(','));
            }
        }
    }
    private bool Trim() {
        bool startedNewLine = ptr >= tokens.Length || tokens[ptr] == '\n';
        while (ptr < tokens.Length && char.IsWhiteSpace(tokens[ptr])) {
            ptr++;
            if (ptr >= tokens.Length || tokens[ptr] == '\n') {
                startedNewLine = true;
            }
        }
        // Skip comments
        while (ptr < tokens.Length && tokens[ptr] == '#') {
            while (ptr < tokens.Length && tokens[ptr] != '\n') {
                ptr++;
                if (ptr >= tokens.Length || tokens[ptr] == '\n') {
                    startedNewLine = true;
                }
            }
            
            startedNewLine |= Trim();
        }
        return startedNewLine;
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
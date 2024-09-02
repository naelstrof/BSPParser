using System.Text;

namespace BSPParser;

public class SentenceTokenizer : Dictionary<string,string> {
    private string tokens;
    private int ptr = 0;

    public SentenceTokenizer(string tokens) {
        this.tokens = tokens;
        List<string> buffer = new List<string>();
        while (ptr < this.tokens.Length) {
            buffer.Clear();
            while (!Trim() && TryParseString(out var token)) {
                buffer.Add(token);
            }
            if (buffer.Count == 2) {
                TryAdd(buffer[0], buffer[1].Trim(','));
            } else if (buffer.Count > 2) {
                string rootPath = buffer[1];
		if (buffer[1].LastIndexOf('/') != -1) {
		    rootPath = buffer[1].Substring(0,buffer[1].LastIndexOf('/'));
		}
                TryAdd(buffer[0], buffer[1].Trim(','));
                for (int i = 1; i < buffer.Count; i++) {
                    TryAdd(buffer[0], $"{rootPath}/{buffer[1].Trim(',')}");
                }
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
        while (ptr < tokens.Length-1 && tokens[ptr] == '/' && tokens[ptr + 1] == '/') {
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
}

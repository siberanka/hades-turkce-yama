using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HadesTurkce
{
    // Reads text data only. Lua is never evaluated.
    public sealed class TextEntry
    {
        public string id, value;
        public int start, length;
    }
    public sealed class RebasedText
    {
        public byte[] data;
        public int applied;
    }
    public static class TextCatalog
    {
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        static readonly Regex Tokens = new Regex(
            "\"\"\".*?\"\"\"|--\\[(=*)\\[.*?\\]\\1\\]|--[^\\r\\n]*|/\\*.*?\\*/|//[^\\r\\n]*|\\[(=*)\\[.*?\\]\\2\\]|\"(?:\\\\.|[^\"\\\\])*\"|'(?:\\\\.|[^'\\\\])*'|[A-Za-z_][\\w.]*|[{}\\[\\]=,:]",
            RegexOptions.Singleline, TimeSpan.FromSeconds(10));
        static readonly Regex Ident = new Regex("^[A-Za-z][A-Za-z0-9_]*$");
        sealed class Token { public string value; public int start, length; }
        static string Quoted(string value)
        {
            if (value.Length < 2 || (value[0] != '"' && value[0] != '\'') || value[value.Length - 1] != value[0])
                throw new FormatException("Invalid quoted text");
            var result = new StringBuilder();
            for (int i = 1; i < value.Length - 1; i++)
            {
                char c = value[i];
                if (c != '\\')
                {
                    if (c == value[0]) throw new FormatException("Unescaped quote");
                    result.Append(c); continue;
                }
                if (++i >= value.Length - 1) throw new FormatException("Incomplete escape");
                c = value[i];
                switch (c)
                {
                    case '"': case '\\': case '/': result.Append(c); break;
                    case 'b': result.Append('\b'); break;
                    case 'f': result.Append('\f'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    case 'u':
                        int code;
                        if (i + 4 >= value.Length - 1 || !Int32.TryParse(value.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber, null, out code))
                            throw new FormatException("Invalid Unicode escape");
                        result.Append((char)code); i += 4; break;
                    default: result.Append('\\').Append(c); break; // SJSON literal escapes
                }
            }
            Utf8.GetByteCount(result.ToString());
            return result.ToString();
        }
        static List<TextEntry> Objects(string text, bool lua)
        {
            var matches = Tokens.Matches(text).Cast<Match>().ToArray();
            int cursor = 0;
            foreach (var match in matches)
            {
                if (match.Index + 3 <= text.Length && text.Substring(match.Index, 3) == "\"\"\"" && !match.Value.StartsWith("\"\"\""))
                    throw new FormatException("Unclosed raw string");
                CheckGap(text.Substring(cursor, match.Index - cursor));
                cursor = match.Index + match.Length;
            }
            CheckGap(text.Substring(cursor));
            var tokens = matches
                .Where(m => !m.Value.StartsWith("--") && !m.Value.StartsWith("/*") && !m.Value.StartsWith("//"))
                .Select(m => new Token { value = m.Value, start = m.Index, length = m.Length }).ToArray();
            var stack = new Stack<Dictionary<string, Token>>();
            var entries = new List<TextEntry>();
            for (int i = 0; i < tokens.Length; i++)
            {
                var t = tokens[i];
                if (t.value == "{") stack.Push(new Dictionary<string, Token>());
                else if (t.value == "}")
                {
                    if (stack.Count == 0) throw new FormatException("Unbalanced object");
                    var obj = stack.Pop();
                    string idField = lua ? "Cue" : "Id";
                    if (!obj.ContainsKey(idField) || obj[idField].value.StartsWith("\"\"\"")) continue;
                    string id = Quoted(obj[idField].value);
                    foreach (string field in lua ? new[] { "Text" } : new[] { "AdditionalDescription", "Description", "DisplayName" })
                    {
                        Token cell;
                        if (!obj.TryGetValue(field, out cell) || cell.value.StartsWith("\"\"\"")) continue;
                        entries.Add(new TextEntry { id = id + "::" + field, value = Quoted(cell.value), start = cell.start, length = cell.length });
                    }
                }
                else if (stack.Count > 0 && i + 2 < tokens.Length && (tokens[i + 1].value == "=" || tokens[i + 1].value == ":") &&
                    (tokens[i + 2].value.StartsWith("\"") || tokens[i + 2].value.StartsWith("'")))
                    stack.Peek()[t.value.Trim('"')] = tokens[i + 2];
            }
            if (stack.Count != 0) throw new FormatException("Unclosed object");
            return entries;
        }
        static void CheckGap(string gap)
        {
            if (gap.IndexOf('"') >= 0 || gap.IndexOf('\'') >= 0 || gap.Contains("/*") || gap.Contains("--"))
                throw new FormatException("Unrecognized string or comment");
        }
        static string CsvCell(string text, int start, int end)
        {
            string value = text.Substring(start, end - start);
            if (value.StartsWith("\""))
            {
                if (value.Length < 2 || !value.EndsWith("\"")) throw new FormatException("Unclosed CSV cell");
                string body = value.Substring(1, value.Length - 2);
                for (int i = 0; i < body.Length; i++)
                    if (body[i] == '"' && (++i >= body.Length || body[i] != '"'))
                        throw new FormatException("Invalid CSV quote");
                return body.Replace("\"\"", "\"");
            }
            if (value.Contains("\"")) throw new FormatException("Invalid CSV quote");
            return value;
        }
        static List<TextEntry> Csv(string text)
        {
            var result = new List<TextEntry>();
            var cells = new List<TextEntry>();
            int start = 0, column = -1;
            bool quoted = false;
            for (int i = 0; i <= text.Length; i++)
            {
                char c = i == text.Length ? '\n' : text[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < text.Length && text[i + 1] == '"') { i++; continue; }
                    quoted = !quoted;
                }
                if (quoted || (c != ',' && c != '\r' && c != '\n')) continue;
                cells.Add(new TextEntry { start = start, length = i - start, value = CsvCell(text, start, i) });
                if (c != ',')
                {
                    if (cells.Count >= 7)
                    {
                        if (column < 0)
                        {
                            column = cells.FindIndex(cell => cell.value == "Line");
                            if (column < 0) throw new FormatException("Missing subtitle Line column");
                        }
                        else if (column < cells.Count && Ident.IsMatch(cells[3].value) && !String.IsNullOrWhiteSpace(cells[column].value))
                        {
                            var cell = cells[column]; cell.id = cells[3].value + "::Line"; result.Add(cell);
                        }
                    }
                    cells.Clear();
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                }
                start = i + 1;
            }
            if (quoted) throw new FormatException("Unclosed CSV quote");
            return result;
        }
        public static List<TextEntry> Read(byte[] data, string path)
        {
            int bom = data.Length >= 3 && data[0] == 239 && data[1] == 187 && data[2] == 191 ? 3 : 0;
            string text = Utf8.GetString(data, bom, data.Length - bom);
            var entries = path.EndsWith(".csv") ? Csv(text) : Objects(text, path.EndsWith(".lua"));
            int[] offsets = new int[text.Length + 1]; offsets[0] = bom;
            for (int i = 0; i < text.Length; i++)
            {
                if (Char.IsHighSurrogate(text[i]))
                { offsets[i + 1] = offsets[i]; offsets[i + 2] = offsets[i] + 4; i++; }
                else offsets[i + 1] = offsets[i] + (text[i] < 128 ? 1 : text[i] < 2048 ? 2 : 3);
            }
            foreach (var entry in entries)
            { int end = offsets[entry.start + entry.length]; entry.start = offsets[entry.start]; entry.length = end - entry.start; }
            return entries.OrderBy(e => e.start).ToList();
        }
        static string Key(string id, string hash) { return id + "\n" + hash; }
        static string SourceHash(string value) { return Engine.Hash(Utf8.GetBytes(value)); }
        static string BaseId(string id)
        {
            int end = id == null ? -1 : id.LastIndexOf("::", StringComparison.Ordinal);
            int occurrence;
            if (end < 1 || !Int32.TryParse(id.Substring(end + 2), out occurrence) || occurrence < 1)
                throw new IOException("Geçersiz metin eşleştirme kimliği.");
            return id.Substring(0, end);
        }
        public static RebasedText Rebase(byte[] original, PatchFile file)
        {
            var expected = file.edits.GroupBy(e => Key(BaseId(e.entry), e.source_sha256)).ToDictionary(g => g.Key, g => g.ToArray());
            var before = Read(original, file.path);
            var actual = before.GroupBy(e => Key(e.id, SourceHash(e.value))).ToDictionary(g => g.Key, g => g.ToArray());
            var replacements = new Dictionary<int, byte[]>();
            var values = new Dictionary<int, string>();
            foreach (var group in expected)
            {
                TextEntry[] matching;
                if (!actual.TryGetValue(group.Key, out matching) || matching.Length != group.Value.Length ||
                    group.Value.Select(e => e.replacement).Distinct().Count() != 1) continue;
                byte[] bytes = Convert.FromBase64String(group.Value[0].replacement);
                if (bytes.Length > 1024 * 1024) throw new IOException("Metin boyutu beklenen sınırı aşıyor.");
                string encoded = Utf8.GetString(bytes);
                string value = file.path.EndsWith(".csv") ? CsvCell(encoded, 0, encoded.Length) : Quoted(encoded);
                foreach (var entry in matching) { replacements.Add(entry.start, bytes); values.Add(entry.start, value); }
            }
            using (var stream = new MemoryStream())
            {
                int cursor = 0;
                foreach (var entry in before)
                {
                    byte[] replacement;
                    if (!replacements.TryGetValue(entry.start, out replacement)) continue;
                    stream.Write(original, cursor, entry.start - cursor); stream.Write(replacement, 0, replacement.Length);
                    cursor = entry.start + entry.length;
                    if (stream.Length > 32 * 1024 * 1024) throw new IOException("Yama boyutu sınırı aşıyor.");
                }
                stream.Write(original, cursor, original.Length - cursor);
                byte[] output = stream.ToArray();
                if (output.Length > 32 * 1024 * 1024) throw new IOException("Yama boyutu sınırı aşıyor.");
                var after = Read(output, file.path);
                if (before.Count != after.Count) throw new IOException("Metin yapısı değişti; dosya korunuyor.");
                for (int i = 0; i < before.Count; i++)
                {
                    string value;
                    if (!values.TryGetValue(before[i].start, out value)) value = before[i].value;
                    if (before[i].id != after[i].id || value != after[i].value)
                        throw new IOException("Metin eşleştirmesi doğrulanamadı; dosya korunuyor.");
                }
                return new RebasedText { data = output, applied = replacements.Count };
            }
        }
    }
}

using System.Globalization;

namespace MafaliCrm.Web.Services;

public static class FiltreReelParser
{
    public static (string Predicate, object[] Args) Parse(string filtreReel, DateOnly today) =>
        Parse(filtreReel, LegacyNameToFieldKey, ClientFilterService.Fields, ClientFilterService.PropertyName, today);

    public static (string Predicate, object[] Args) ParseHistorique(string filtreReel, DateOnly today) =>
        Parse(filtreReel, HistoriqueLegacyNameToFieldKey, HistoriqueFilterService.Fields, HistoriqueFilterService.PropertyName, today);

    private static (string Predicate, object[] Args) Parse(
        string filtreReel,
        Dictionary<string, string> legacyNameToFieldKey,
        List<FilterField> fields,
        Dictionary<string, string> propertyName,
        DateOnly today)
    {
        var tokens = Tokenize(filtreReel);
        var pos = 0;
        var args = new List<object>();
        var predicate = ParseOr(tokens, ref pos, args, filtreReel, legacyNameToFieldKey, fields, propertyName, today);
        if (pos != tokens.Count)
        {
            throw new FormatException($"Unexpected \"{tokens[pos].Text}\" in filter: \"{filtreReel}\"");
        }
        return (predicate, args.ToArray());
    }

    private enum TokenType { LParen, RParen, Ident, String, Op, Like, Not, Between, And, Et, Ou, Sysdate }

    private record Token(TokenType Type, string Text);

    private static List<Token> Tokenize(string s)
    {
        var tokens = new List<Token>();
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c == '(') { tokens.Add(new(TokenType.LParen, "(")); i++; continue; }
            if (c == ')') { tokens.Add(new(TokenType.RParen, ")")); i++; continue; }

            if (c == '\'')
            {
                var j = i + 1;
                while (j < s.Length && s[j] != '\'') j++;
                if (j >= s.Length)
                {
                    throw new FormatException($"Unterminated string in filter: \"{s}\"");
                }
                tokens.Add(new(TokenType.String, s.Substring(i + 1, j - i - 1)));
                i = j + 1;
                continue;
            }

            if (c == '<')
            {
                if (i + 1 < s.Length && s[i + 1] == '>') { tokens.Add(new(TokenType.Op, "<>")); i += 2; }
                else if (i + 1 < s.Length && s[i + 1] == '=') { tokens.Add(new(TokenType.Op, "<=")); i += 2; }
                else { tokens.Add(new(TokenType.Op, "<")); i++; }
                continue;
            }
            if (c == '>')
            {
                if (i + 1 < s.Length && s[i + 1] == '=') { tokens.Add(new(TokenType.Op, ">=")); i += 2; }
                else { tokens.Add(new(TokenType.Op, ">")); i++; }
                continue;
            }
            if (c == '=') { tokens.Add(new(TokenType.Op, "=")); i++; continue; }

            if (char.IsLetter(c) || c == '_')
            {
                var j = i;
                while (j < s.Length && (char.IsLetterOrDigit(s[j]) || s[j] == '_')) j++;
                var word = s.Substring(i, j - i);
                i = j;
                tokens.Add(word.ToUpperInvariant() switch
                {
                    "ET" => new Token(TokenType.Et, word),
                    "OU" => new Token(TokenType.Ou, word),
                    "LIKE" => new Token(TokenType.Like, word),
                    "NOT" => new Token(TokenType.Not, word),
                    "BETWEEN" => new Token(TokenType.Between, word),
                    "AND" => new Token(TokenType.And, word),
                    "SYSDATE" => new Token(TokenType.Sysdate, word),
                    _ => new Token(TokenType.Ident, word),
                });
                continue;
            }

            throw new FormatException($"Unexpected character '{c}' in filter: \"{s}\"");
        }
        return tokens;
    }

    private static string ParseOr(List<Token> tokens, ref int pos, List<object> args, string original,
        Dictionary<string, string> legacyNameToFieldKey, List<FilterField> fields, Dictionary<string, string> propertyName,
        DateOnly today)
    {
        var parts = new List<string> { ParseAnd(tokens, ref pos, args, original, legacyNameToFieldKey, fields, propertyName, today) };
        while (pos < tokens.Count && tokens[pos].Type == TokenType.Ou)
        {
            pos++;
            parts.Add(ParseAnd(tokens, ref pos, args, original, legacyNameToFieldKey, fields, propertyName, today));
        }
        return parts.Count == 1 ? parts[0] : "(" + string.Join(" || ", parts) + ")";
    }

    private static string ParseAnd(List<Token> tokens, ref int pos, List<object> args, string original,
        Dictionary<string, string> legacyNameToFieldKey, List<FilterField> fields, Dictionary<string, string> propertyName,
        DateOnly today)
    {
        var parts = new List<string> { ParseTerm(tokens, ref pos, args, original, legacyNameToFieldKey, fields, propertyName, today) };
        while (pos < tokens.Count && tokens[pos].Type == TokenType.Et)
        {
            pos++;
            parts.Add(ParseTerm(tokens, ref pos, args, original, legacyNameToFieldKey, fields, propertyName, today));
        }
        return parts.Count == 1 ? parts[0] : "(" + string.Join(" && ", parts) + ")";
    }

    private static string ParseTerm(List<Token> tokens, ref int pos, List<object> args, string original,
        Dictionary<string, string> legacyNameToFieldKey, List<FilterField> fields, Dictionary<string, string> propertyName,
        DateOnly today)
    {
        if (pos >= tokens.Count)
        {
            throw new FormatException($"Unexpected end of filter: \"{original}\"");
        }
        if (tokens[pos].Type == TokenType.LParen)
        {
            pos++;
            var inner = ParseOr(tokens, ref pos, args, original, legacyNameToFieldKey, fields, propertyName, today);
            Expect(tokens, ref pos, TokenType.RParen, original);
            return inner;
        }
        return ParseComparison(tokens, ref pos, args, original, legacyNameToFieldKey, fields, propertyName, today);
    }

    private static string ParseComparison(List<Token> tokens, ref int pos, List<object> args, string original,
        Dictionary<string, string> legacyNameToFieldKey, List<FilterField> fields, Dictionary<string, string> propertyName,
        DateOnly today)
    {
        var fieldToken = Expect(tokens, ref pos, TokenType.Ident, original);
        if (!legacyNameToFieldKey.TryGetValue(fieldToken.Text.ToUpperInvariant(), out var fieldKey))
        {
            throw new FormatException($"Unknown field \"{fieldToken.Text}\" in filter: \"{original}\"");
        }
        var meta = fields.First(f => f.Field == fieldKey);
        var prop = propertyName[fieldKey];

        if (pos < tokens.Count && tokens[pos].Type == TokenType.Between)
        {
            pos++;
            var v1 = CastLegacyValue(meta.Type, ParseValueToken(tokens, ref pos, original), original, today);
            Expect(tokens, ref pos, TokenType.And, original);
            var v2 = CastLegacyValue(meta.Type, ParseValueToken(tokens, ref pos, original), original, today);
            var i1 = args.Count; args.Add(v1);
            var i2 = args.Count; args.Add(v2);
            return $"({prop} >= @{i1} && {prop} <= @{i2})";
        }

        Token opToken;
        var negated = false;
        if (pos < tokens.Count && tokens[pos].Type == TokenType.Not)
        {
            negated = true;
            pos++;
            opToken = Expect(tokens, ref pos, TokenType.Like, original);
        }
        else
        {
            opToken = Expect(tokens, ref pos, TokenType.Op, TokenType.Like, original);
            if (opToken.Type == TokenType.Op && opToken.Text == "<>") negated = true;
        }

        var raw = ParseValueToken(tokens, ref pos, original);

        if (meta.Type == FieldType.Text && raw.Contains('%'))
        {
            return BuildWildcardClause(prop, raw, negated, args);
        }

        if (meta.Type == FieldType.Text && raw == "")
        {
            return BuildBlankClause(prop, MapComparisonOperator(opToken, negated), args);
        }

        var value = CastLegacyValue(meta.Type, raw, original, today);
        var index = args.Count; args.Add(value);
        return ClientFilterService.OperatorTemplate(prop, ClientFilterService.EffectiveOperator(MapComparisonOperator(opToken, negated), raw), $"@{index}", meta.Type, args);
    }

    private static string BuildBlankClause(string prop, Operator op, List<object> args)
    {
        var index = args.Count; args.Add(string.Empty);
        return op switch
        {
            Operator.Eq => $"({prop} == null || {prop} == @{index})",
            Operator.Ne => $"({prop} != null && {prop} != @{index})",
            _ => throw new FormatException($"Unsupported operator for a blank ('') comparison on field \"{prop}\"."),
        };
    }

    private static Operator MapComparisonOperator(Token opToken, bool negated) => opToken switch
    {
        { Type: TokenType.Op, Text: "=" } => Operator.Eq,
        { Type: TokenType.Op, Text: "<>" } => Operator.Ne,
        { Type: TokenType.Op, Text: "<" } => Operator.Lt,
        { Type: TokenType.Op, Text: "<=" } => Operator.Lte,
        { Type: TokenType.Op, Text: ">" } => Operator.Gt,
        { Type: TokenType.Op, Text: ">=" } => Operator.Gte,
        { Type: TokenType.Like } => negated ? Operator.Ne : Operator.Eq,
        _ => throw new FormatException($"Unsupported operator \"{opToken.Text}\"."),
    };

    private static string BuildWildcardClause(string prop, string raw, bool negated, List<object> args)
    {
        if (raw.Length > 0 && raw.All(c => c == '%'))
        {
            return negated ? "false" : "true";
        }

        var leading = raw.StartsWith('%');
        var trailing = raw.EndsWith('%');
        var start = leading ? 1 : 0;
        var end = raw.Length - (trailing ? 1 : 0);
        var stripped = end > start ? raw[start..end] : "";

        Operator op;
        if (leading && trailing) op = negated ? Operator.NotContains : Operator.Contains;
        else if (trailing) op = negated ? Operator.NotStartsWith : Operator.StartsWith;
        else throw new FormatException($"Unsupported wildcard position in value \"{raw}\" (only \"text%\" and \"%text%\" are supported).");

        var index = args.Count; args.Add(stripped);
        return ClientFilterService.OperatorTemplate(prop, op, $"@{index}", FieldType.Text, args);
    }

    private static string ParseValueToken(List<Token> tokens, ref int pos, string original)
    {
        if (pos >= tokens.Count)
        {
            throw new FormatException($"Expected a value in filter: \"{original}\"");
        }
        var t = tokens[pos];
        if (t.Type == TokenType.String) { pos++; return t.Text; }
        if (t.Type == TokenType.Sysdate) { pos++; return "SYSDATE"; }
        throw new FormatException($"Expected a value but found \"{t.Text}\" in filter: \"{original}\"");
    }

    private static object CastLegacyValue(FieldType type, string raw, string original, DateOnly today)
    {
        if (raw == "SYSDATE")
        {
            if (type != FieldType.Date)
            {
                throw new FormatException($"SYSDATE used on a non-date field in filter: \"{original}\"");
            }
            return today;
        }

        return type switch
        {
            FieldType.Date => DateOnly.ParseExact(raw, "yyyyMMdd", CultureInfo.InvariantCulture),
            FieldType.Number => long.Parse(raw),
            FieldType.Time => TimeOnly.ParseExact(raw, "HHmm", CultureInfo.InvariantCulture),
            FieldType.Boolean => raw is "1" or "Vrai" or "VRAI",
            _ => raw,
        };
    }

    private static Token Expect(List<Token> tokens, ref int pos, TokenType type, string original)
    {
        if (pos >= tokens.Count || tokens[pos].Type != type)
        {
            var found = pos < tokens.Count ? tokens[pos].Text : "end of filter";
            throw new FormatException($"Expected {type} but found \"{found}\" in filter: \"{original}\"");
        }
        return tokens[pos++];
    }

    private static Token Expect(List<Token> tokens, ref int pos, TokenType a, TokenType b, string original)
    {
        if (pos >= tokens.Count || (tokens[pos].Type != a && tokens[pos].Type != b))
        {
            var found = pos < tokens.Count ? tokens[pos].Text : "end of filter";
            throw new FormatException($"Expected an operator but found \"{found}\" in filter: \"{original}\"");
        }
        return tokens[pos++];
    }

    private static readonly Dictionary<string, string> LegacyNameToFieldKey =
        ClientFilterService.LegacyFieldName.ToDictionary(
            kv => kv.Value.ToUpperInvariant(),
            kv => kv.Key);

    private static readonly Dictionary<string, string> HistoriqueLegacyNameToFieldKey =
        HistoriqueFilterService.LegacyFieldName.ToDictionary(
            kv => kv.Value.ToUpperInvariant(),
            kv => kv.Key);
}

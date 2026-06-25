using System.Globalization;
using System.Text;

namespace MoonByte.Core;

public sealed class Lexer
{
    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.Ordinal)
    {
        ["let"] = TokenType.Let,
        ["fn"] = TokenType.Fn,
        ["return"] = TokenType.Return,
        ["true"] = TokenType.True,
        ["false"] = TokenType.False,
        ["nil"] = TokenType.Nil
    };

    private readonly string _source;
    private readonly List<Token> _tokens = [];
    private int _start;
    private int _current;
    private int _line = 1;
    private int _column = 1;
    private int _tokenColumn = 1;

    public Lexer(string source)
    {
        _source = source;
    }

    public IReadOnlyList<Token> Lex()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            _tokenColumn = _column;
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.Eof, string.Empty, null, _line, _column));
        return _tokens;
    }

    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            case '(':
                Add(TokenType.LeftParen);
                break;
            case ')':
                Add(TokenType.RightParen);
                break;
            case '{':
                Add(TokenType.LeftBrace);
                break;
            case '}':
                Add(TokenType.RightBrace);
                break;
            case ',':
                Add(TokenType.Comma);
                break;
            case '.':
                Add(TokenType.Dot);
                break;
            case ';':
                Add(TokenType.Semicolon);
                break;
            case ':':
                Add(TokenType.Colon);
                break;
            case '+':
                Add(TokenType.Plus);
                break;
            case '*':
                Add(TokenType.Star);
                break;
            case '!':
                Add(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                break;
            case '=':
                Add(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                break;
            case '<':
                Add(Match('=') ? TokenType.LessEqual : TokenType.Less);
                break;
            case '>':
                Add(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                break;
            case '-':
                if (Match('-'))
                {
                    SkipLineComment();
                }
                else
                {
                    Add(TokenType.Minus);
                }
                break;
            case '/':
                if (Match('/'))
                {
                    SkipLineComment();
                }
                else
                {
                    Add(TokenType.Slash);
                }
                break;
            case ' ':
            case '\r':
            case '\t':
                break;
            case '\n':
                NewLine();
                break;
            case '"':
                String();
                break;
            default:
                if (char.IsDigit(c))
                {
                    Number();
                }
                else if (IsIdentifierStart(c))
                {
                    Identifier();
                }
                else
                {
                    throw Error($"unexpected character '{c}'");
                }

                break;
        }
    }

    private void Identifier()
    {
        while (IsIdentifierPart(Peek()))
        {
            Advance();
        }

        string text = _source[_start.._current];
        Add(Keywords.TryGetValue(text, out TokenType type) ? type : TokenType.Identifier);
    }

    private void Number()
    {
        while (char.IsDigit(Peek()))
        {
            Advance();
        }

        if (Peek() == '.' && char.IsDigit(PeekNext()))
        {
            Advance();
            while (char.IsDigit(Peek()))
            {
                Advance();
            }
        }

        string text = _source[_start.._current];
        double value = double.Parse(text, CultureInfo.InvariantCulture);
        Add(TokenType.Number, value);
    }

    private void String()
    {
        var builder = new StringBuilder();
        while (!IsAtEnd() && Peek() != '"')
        {
            char c = Advance();
            if (c == '\n')
            {
                NewLine();
                builder.Append('\n');
                continue;
            }

            if (c == '\\')
            {
                if (IsAtEnd())
                {
                    throw Error("unterminated escape sequence");
                }

                char escaped = Advance();
                builder.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '"' => '"',
                    '\\' => '\\',
                    _ => throw Error($"unsupported escape '\\{escaped}'")
                });
            }
            else
            {
                builder.Append(c);
            }
        }

        if (IsAtEnd())
        {
            throw Error("unterminated string literal");
        }

        Advance();
        Add(TokenType.String, builder.ToString());
    }

    private void SkipLineComment()
    {
        while (!IsAtEnd() && Peek() != '\n')
        {
            Advance();
        }
    }

    private char Advance()
    {
        _current++;
        _column++;
        return _source[_current - 1];
    }

    private bool Match(char expected)
    {
        if (IsAtEnd() || _source[_current] != expected)
        {
            return false;
        }

        _current++;
        _column++;
        return true;
    }

    private char Peek() => IsAtEnd() ? '\0' : _source[_current];

    private char PeekNext() => _current + 1 >= _source.Length ? '\0' : _source[_current + 1];

    private bool IsAtEnd() => _current >= _source.Length;

    private void Add(TokenType type, object? literal = null)
    {
        string text = _source[_start.._current];
        _tokens.Add(new Token(type, text, literal, _line, _tokenColumn));
    }

    private void NewLine()
    {
        _line++;
        _column = 1;
    }

    private MoonByteException Error(string message) => new($"line {_line}, column {_tokenColumn}: {message}");

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c) => IsIdentifierStart(c) || char.IsDigit(c);
}


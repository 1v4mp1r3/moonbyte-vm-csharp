namespace MoonByte.Core;

public enum TokenType
{
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    Comma,
    Dot,
    Semicolon,
    Colon,
    Plus,
    Minus,
    Star,
    Slash,
    Bang,
    BangEqual,
    Equal,
    EqualEqual,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,
    Identifier,
    Number,
    String,
    Let,
    Fn,
    Return,
    True,
    False,
    Nil,
    Eof
}

public sealed record Token(TokenType Type, string Lexeme, object? Literal, int Line, int Column);


namespace MoonByte.Core;

public sealed class Parser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _current;

    public Parser(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
    }

    public ProgramNode Parse()
    {
        var statements = new List<Statement>();
        while (!IsAtEnd())
        {
            statements.Add(Declaration());
        }

        return new ProgramNode(statements);
    }

    private Statement Declaration()
    {
        if (Match(TokenType.Fn))
        {
            return FunctionDeclaration();
        }

        return Statement();
    }

    private FunctionStatement FunctionDeclaration()
    {
        Token name = Consume(TokenType.Identifier, "expected function name");
        Consume(TokenType.LeftParen, "expected '(' after function name");

        var parameters = new List<string>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                parameters.Add(Consume(TokenType.Identifier, "expected parameter name").Lexeme);
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "expected ')' after parameters");
        IReadOnlyList<Statement> body = Block();
        return new FunctionStatement(name.Lexeme, parameters, body);
    }

    private Statement Statement()
    {
        if (Match(TokenType.Let))
        {
            return LetStatement();
        }

        if (Match(TokenType.Return))
        {
            return ReturnStatement();
        }

        return ExpressionStatement();
    }

    private LetStatement LetStatement()
    {
        Token name = Consume(TokenType.Identifier, "expected variable name");
        Consume(TokenType.Equal, "expected '=' after variable name");
        Expression value = Expression();
        OptionalSemicolon();
        return new LetStatement(name.Lexeme, value);
    }

    private ReturnStatement ReturnStatement()
    {
        Expression? value = null;
        if (!Check(TokenType.Semicolon) && !Check(TokenType.RightBrace) && !Check(TokenType.Eof))
        {
            value = Expression();
        }

        OptionalSemicolon();
        return new ReturnStatement(value);
    }

    private ExpressionStatement ExpressionStatement()
    {
        Expression expr = Expression();
        OptionalSemicolon();
        return new ExpressionStatement(expr);
    }

    private IReadOnlyList<Statement> Block()
    {
        Consume(TokenType.LeftBrace, "expected '{' before block");
        var statements = new List<Statement>();
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            statements.Add(Declaration());
        }

        Consume(TokenType.RightBrace, "expected '}' after block");
        return statements;
    }

    private Expression Expression() => Equality();

    private Expression Equality()
    {
        Expression expr = Comparison();
        while (Match(TokenType.BangEqual, TokenType.EqualEqual))
        {
            Token op = Previous();
            Expression right = Comparison();
            expr = new BinaryExpression(expr, op.Type, right);
        }

        return expr;
    }

    private Expression Comparison()
    {
        Expression expr = Term();
        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            Token op = Previous();
            Expression right = Term();
            expr = new BinaryExpression(expr, op.Type, right);
        }

        return expr;
    }

    private Expression Term()
    {
        Expression expr = Factor();
        while (Match(TokenType.Minus, TokenType.Plus))
        {
            Token op = Previous();
            Expression right = Factor();
            expr = new BinaryExpression(expr, op.Type, right);
        }

        return expr;
    }

    private Expression Factor()
    {
        Expression expr = Unary();
        while (Match(TokenType.Slash, TokenType.Star))
        {
            Token op = Previous();
            Expression right = Unary();
            expr = new BinaryExpression(expr, op.Type, right);
        }

        return expr;
    }

    private Expression Unary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            Token op = Previous();
            Expression right = Unary();
            return new UnaryExpression(op.Type, right);
        }

        return Call();
    }

    private Expression Call()
    {
        Expression expr = Primary();
        while (true)
        {
            if (Match(TokenType.LeftParen))
            {
                var args = new List<Expression>();
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        args.Add(Expression());
                    } while (Match(TokenType.Comma));
                }

                Consume(TokenType.RightParen, "expected ')' after arguments");
                expr = new CallExpression(expr, args);
            }
            else if (Match(TokenType.Dot))
            {
                string name = Consume(TokenType.Identifier, "expected property name").Lexeme;
                expr = new PropertyExpression(expr, name);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private Expression Primary()
    {
        if (Match(TokenType.False))
        {
            return new LiteralExpression(MbValue.False);
        }

        if (Match(TokenType.True))
        {
            return new LiteralExpression(MbValue.True);
        }

        if (Match(TokenType.Nil))
        {
            return new LiteralExpression(MbValue.Nil);
        }

        if (Match(TokenType.Number))
        {
            return new LiteralExpression(MbValue.Number((double)Previous().Literal!));
        }

        if (Match(TokenType.String))
        {
            return new LiteralExpression(MbValue.String((string)Previous().Literal!));
        }

        if (Match(TokenType.Identifier))
        {
            return new VariableExpression(Previous().Lexeme);
        }

        if (Match(TokenType.LeftParen))
        {
            Expression expr = Expression();
            Consume(TokenType.RightParen, "expected ')' after expression");
            return expr;
        }

        if (Match(TokenType.LeftBrace))
        {
            return TableLiteral();
        }

        throw Error(Peek(), "expected expression");
    }

    private TableExpression TableLiteral()
    {
        var fields = new List<TableField>();
        if (!Check(TokenType.RightBrace))
        {
            do
            {
                Token key = Match(TokenType.String)
                    ? Previous()
                    : Consume(TokenType.Identifier, "expected table field name");
                Consume(TokenType.Colon, "expected ':' after table field name");
                fields.Add(new TableField(key.Literal?.ToString() ?? key.Lexeme, Expression()));
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightBrace, "expected '}' after table literal");
        return new TableExpression(fields);
    }

    private bool Match(params TokenType[] types)
    {
        foreach (TokenType type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        throw Error(Peek(), message);
    }

    private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;

    private Token Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }

        return Previous();
    }

    private bool IsAtEnd() => Peek().Type == TokenType.Eof;

    private Token Peek() => _tokens[_current];

    private Token Previous() => _tokens[_current - 1];

    private void OptionalSemicolon()
    {
        _ = Match(TokenType.Semicolon);
    }

    private static MoonByteException Error(Token token, string message) =>
        new($"line {token.Line}, column {token.Column}: {message}");
}


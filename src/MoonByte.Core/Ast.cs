namespace MoonByte.Core;

public sealed record ProgramNode(IReadOnlyList<Statement> Statements);

public abstract record Statement;

public sealed record LetStatement(string Name, Expression Value) : Statement;

public sealed record ExpressionStatement(Expression Expression) : Statement;

public sealed record ReturnStatement(Expression? Value) : Statement;

public sealed record FunctionStatement(string Name, IReadOnlyList<string> Parameters, IReadOnlyList<Statement> Body) : Statement;

public abstract record Expression;

public sealed record LiteralExpression(MbValue Value) : Expression;

public sealed record VariableExpression(string Name) : Expression;

public sealed record UnaryExpression(TokenType Operator, Expression Right) : Expression;

public sealed record BinaryExpression(Expression Left, TokenType Operator, Expression Right) : Expression;

public sealed record CallExpression(Expression Callee, IReadOnlyList<Expression> Arguments) : Expression;

public sealed record TableExpression(IReadOnlyList<TableField> Fields) : Expression;

public sealed record PropertyExpression(Expression Target, string Name) : Expression;

public sealed record TableField(string Name, Expression Value);


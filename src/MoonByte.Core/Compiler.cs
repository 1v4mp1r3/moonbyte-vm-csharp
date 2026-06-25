namespace MoonByte.Core;

public sealed class Compiler
{
    public BytecodeChunk Compile(ProgramNode program)
    {
        var context = new CompileContext("<script>", isFunction: false, parameters: []);
        foreach (Statement statement in program.Statements)
        {
            CompileStatement(context, statement);
        }

        context.Chunk.Emit(OpCode.Nil);
        context.Chunk.Emit(OpCode.Return);
        return context.Chunk;
    }

    private void CompileStatement(CompileContext context, Statement statement)
    {
        switch (statement)
        {
            case LetStatement let:
                CompileExpression(context, let.Value);
                if (context.IsFunction)
                {
                    int slot = context.DeclareLocal(let.Name);
                    context.Chunk.Emit(OpCode.SetLocal, slot);
                }
                else
                {
                    context.Chunk.Emit(OpCode.SetGlobal, name: let.Name);
                }

                break;
            case ExpressionStatement expr:
                CompileExpression(context, expr.Expression);
                context.Chunk.Emit(OpCode.Pop);
                break;
            case ReturnStatement ret:
                if (ret.Value is null)
                {
                    context.Chunk.Emit(OpCode.Nil);
                }
                else
                {
                    CompileExpression(context, ret.Value);
                }

                context.Chunk.Emit(OpCode.Return);
                break;
            case FunctionStatement fn:
                CompileFunction(context, fn);
                break;
            default:
                throw new MoonByteException($"unsupported statement {statement.GetType().Name}");
        }
    }

    private void CompileFunction(CompileContext parent, FunctionStatement fn)
    {
        var context = new CompileContext(fn.Name, isFunction: true, fn.Parameters);
        foreach (Statement statement in fn.Body)
        {
            CompileStatement(context, statement);
        }

        context.Chunk.Emit(OpCode.Nil);
        context.Chunk.Emit(OpCode.Return);
        var function = new MbFunction(fn.Name, fn.Parameters, context.Chunk);
        int constant = parent.Chunk.AddConstant(MbValue.Function(function));
        parent.Chunk.Emit(OpCode.Constant, constant);

        if (parent.IsFunction)
        {
            int slot = parent.DeclareLocal(fn.Name);
            parent.Chunk.Emit(OpCode.SetLocal, slot);
        }
        else
        {
            parent.Chunk.Emit(OpCode.SetGlobal, name: fn.Name);
        }
    }

    private void CompileExpression(CompileContext context, Expression expression)
    {
        switch (expression)
        {
            case LiteralExpression literal:
                EmitLiteral(context, literal.Value);
                break;
            case VariableExpression variable:
                if (context.TryResolveLocal(variable.Name, out int slot))
                {
                    context.Chunk.Emit(OpCode.GetLocal, slot);
                }
                else
                {
                    context.Chunk.Emit(OpCode.GetGlobal, name: variable.Name);
                }

                break;
            case UnaryExpression unary:
                CompileExpression(context, unary.Right);
                context.Chunk.Emit(unary.Operator switch
                {
                    TokenType.Minus => OpCode.Negate,
                    TokenType.Bang => OpCode.Not,
                    _ => throw new MoonByteException($"unsupported unary operator {unary.Operator}")
                });
                break;
            case BinaryExpression binary:
                CompileExpression(context, binary.Left);
                CompileExpression(context, binary.Right);
                context.Chunk.Emit(binary.Operator switch
                {
                    TokenType.Plus => OpCode.Add,
                    TokenType.Minus => OpCode.Subtract,
                    TokenType.Star => OpCode.Multiply,
                    TokenType.Slash => OpCode.Divide,
                    TokenType.EqualEqual => OpCode.Equal,
                    TokenType.BangEqual => OpCode.NotEqual,
                    TokenType.Greater => OpCode.Greater,
                    TokenType.GreaterEqual => OpCode.GreaterEqual,
                    TokenType.Less => OpCode.Less,
                    TokenType.LessEqual => OpCode.LessEqual,
                    _ => throw new MoonByteException($"unsupported binary operator {binary.Operator}")
                });
                break;
            case CallExpression call:
                CompileExpression(context, call.Callee);
                foreach (Expression argument in call.Arguments)
                {
                    CompileExpression(context, argument);
                }

                context.Chunk.Emit(OpCode.Call, call.Arguments.Count);
                break;
            case TableExpression table:
                foreach (TableField field in table.Fields)
                {
                    int key = context.Chunk.AddConstant(MbValue.String(field.Name));
                    context.Chunk.Emit(OpCode.Constant, key);
                    CompileExpression(context, field.Value);
                }

                context.Chunk.Emit(OpCode.MakeTable, table.Fields.Count);
                break;
            case PropertyExpression property:
                CompileExpression(context, property.Target);
                context.Chunk.Emit(OpCode.GetProperty, name: property.Name);
                break;
            default:
                throw new MoonByteException($"unsupported expression {expression.GetType().Name}");
        }
    }

    private static void EmitLiteral(CompileContext context, MbValue value)
    {
        switch (value.Kind)
        {
            case ValueKind.Nil:
                context.Chunk.Emit(OpCode.Nil);
                break;
            case ValueKind.Boolean:
                context.Chunk.Emit(value.IsTruthy() ? OpCode.True : OpCode.False);
                break;
            default:
                int constant = context.Chunk.AddConstant(value);
                context.Chunk.Emit(OpCode.Constant, constant);
                break;
        }
    }

    private sealed class CompileContext
    {
        private readonly Dictionary<string, int> _locals = new(StringComparer.Ordinal);

        public CompileContext(string name, bool isFunction, IReadOnlyList<string> parameters)
        {
            Name = name;
            IsFunction = isFunction;
            foreach (string parameter in parameters)
            {
                DeclareLocal(parameter);
            }
        }

        public string Name { get; }

        public bool IsFunction { get; }

        public BytecodeChunk Chunk { get; } = new();

        public int DeclareLocal(string name)
        {
            if (_locals.ContainsKey(name))
            {
                throw new MoonByteException($"local '{name}' is already declared in {Name}");
            }

            int slot = _locals.Count;
            _locals[name] = slot;
            Chunk.LocalCount = Math.Max(Chunk.LocalCount, slot + 1);
            return slot;
        }

        public bool TryResolveLocal(string name, out int slot) => _locals.TryGetValue(name, out slot);
    }
}


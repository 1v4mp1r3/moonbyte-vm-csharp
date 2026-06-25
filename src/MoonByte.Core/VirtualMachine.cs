using System.Globalization;

namespace MoonByte.Core;

public sealed class VirtualMachine
{
    private readonly Dictionary<string, MbValue> _globals = new(StringComparer.Ordinal);
    private readonly List<string> _output = [];
    private readonly Action<string> _writer;
    private readonly int _maxSteps;

    public VirtualMachine(Action<string>? writer = null, int maxSteps = 100_000)
    {
        _writer = writer ?? (_ => { });
        _maxSteps = maxSteps;
        RegisterBuiltins();
    }

    public IReadOnlyList<string> Output => _output;

    public void SetGlobal(string name, MbValue value) => _globals[name] = value;

    public MbValue GetGlobal(string name) => _globals.TryGetValue(name, out MbValue? value)
        ? value
        : throw new MoonByteException($"undefined global '{name}'");

    public void RegisterHost(string name, HostFunction function) => _globals[name] = MbValue.Host(function);

    public MbValue Execute(BytecodeChunk chunk) => ExecuteChunk(chunk, []);

    private MbValue ExecuteChunk(BytecodeChunk chunk, MbValue[] locals)
    {
        var stack = new Stack<MbValue>();
        int steps = 0;

        for (int ip = 0; ip < chunk.Instructions.Count; ip++)
        {
            if (++steps > _maxSteps)
            {
                throw new MoonByteException($"execution exceeded {_maxSteps} VM steps");
            }

            Instruction instruction = chunk.Instructions[ip];
            switch (instruction.OpCode)
            {
                case OpCode.Constant:
                    stack.Push(chunk.Constants[instruction.Operand]);
                    break;
                case OpCode.Nil:
                    stack.Push(MbValue.Nil);
                    break;
                case OpCode.True:
                    stack.Push(MbValue.True);
                    break;
                case OpCode.False:
                    stack.Push(MbValue.False);
                    break;
                case OpCode.GetGlobal:
                    stack.Push(GetGlobal(RequireName(instruction)));
                    break;
                case OpCode.SetGlobal:
                    _globals[RequireName(instruction)] = Pop(stack, instruction.OpCode);
                    break;
                case OpCode.GetLocal:
                    stack.Push(ReadLocal(locals, instruction.Operand));
                    break;
                case OpCode.SetLocal:
                    WriteLocal(locals, instruction.Operand, Pop(stack, instruction.OpCode));
                    break;
                case OpCode.Add:
                    Add(stack);
                    break;
                case OpCode.Subtract:
                    NumberBinary(stack, (left, right) => left - right);
                    break;
                case OpCode.Multiply:
                    NumberBinary(stack, (left, right) => left * right);
                    break;
                case OpCode.Divide:
                    NumberBinary(stack, (left, right) => left / right);
                    break;
                case OpCode.Negate:
                    stack.Push(MbValue.Number(-Pop(stack, instruction.OpCode).AsNumber()));
                    break;
                case OpCode.Not:
                    stack.Push(MbValue.Boolean(!Pop(stack, instruction.OpCode).IsTruthy()));
                    break;
                case OpCode.Equal:
                    Compare(stack, (left, right) => left.Equals(right));
                    break;
                case OpCode.NotEqual:
                    Compare(stack, (left, right) => !left.Equals(right));
                    break;
                case OpCode.Greater:
                    NumberCompare(stack, (left, right) => left > right);
                    break;
                case OpCode.GreaterEqual:
                    NumberCompare(stack, (left, right) => left >= right);
                    break;
                case OpCode.Less:
                    NumberCompare(stack, (left, right) => left < right);
                    break;
                case OpCode.LessEqual:
                    NumberCompare(stack, (left, right) => left <= right);
                    break;
                case OpCode.Call:
                    stack.Push(Call(stack, instruction.Operand));
                    break;
                case OpCode.Return:
                    return stack.Count == 0 ? MbValue.Nil : stack.Pop();
                case OpCode.Pop:
                    _ = Pop(stack, instruction.OpCode);
                    break;
                case OpCode.MakeTable:
                    stack.Push(MakeTable(stack, instruction.Operand));
                    break;
                case OpCode.GetProperty:
                    MbValue target = Pop(stack, instruction.OpCode);
                    stack.Push(target.AsTable().Get(RequireName(instruction)));
                    break;
                default:
                    throw new MoonByteException($"unsupported opcode {instruction.OpCode}");
            }
        }

        return MbValue.Nil;
    }

    private MbValue Call(Stack<MbValue> stack, int argCount)
    {
        var args = new MbValue[argCount];
        for (int i = argCount - 1; i >= 0; i--)
        {
            args[i] = Pop(stack, OpCode.Call);
        }

        MbValue callee = Pop(stack, OpCode.Call);
        return callee.Kind switch
        {
            ValueKind.Function => CallFunction(callee.AsFunction(), args),
            ValueKind.HostFunction => callee.AsHostFunction()(args),
            _ => throw new MoonByteException($"value {callee.ToDisplayString()} is not callable")
        };
    }

    private MbValue CallFunction(MbFunction function, IReadOnlyList<MbValue> args)
    {
        if (args.Count != function.Parameters.Count)
        {
            throw new MoonByteException($"function {function.Name} expects {function.Parameters.Count} args, got {args.Count}");
        }

        int localCount = Math.Max(function.Chunk.LocalCount, args.Count);
        var locals = Enumerable.Repeat(MbValue.Nil, localCount).ToArray();
        for (int i = 0; i < args.Count; i++)
        {
            locals[i] = args[i];
        }

        return ExecuteChunk(function.Chunk, locals);
    }

    private MbValue MakeTable(Stack<MbValue> stack, int fieldCount)
    {
        var table = new MbTable();
        for (int i = 0; i < fieldCount; i++)
        {
            MbValue value = Pop(stack, OpCode.MakeTable);
            string key = Pop(stack, OpCode.MakeTable).AsString();
            table.Set(key, value);
        }

        return MbValue.Table(table);
    }

    private static string RequireName(Instruction instruction) => instruction.Name
        ?? throw new MoonByteException($"{instruction.OpCode} instruction is missing a name");

    private static MbValue ReadLocal(MbValue[] locals, int slot)
    {
        if (slot < 0 || slot >= locals.Length)
        {
            throw new MoonByteException($"local slot {slot} is out of range");
        }

        return locals[slot];
    }

    private static void WriteLocal(MbValue[] locals, int slot, MbValue value)
    {
        if (slot < 0 || slot >= locals.Length)
        {
            throw new MoonByteException($"local slot {slot} is out of range");
        }

        locals[slot] = value;
    }

    private static MbValue Pop(Stack<MbValue> stack, OpCode opCode)
    {
        if (stack.Count == 0)
        {
            throw new MoonByteException($"stack underflow during {opCode}");
        }

        return stack.Pop();
    }

    private static void Add(Stack<MbValue> stack)
    {
        MbValue right = Pop(stack, OpCode.Add);
        MbValue left = Pop(stack, OpCode.Add);
        if (left.Kind == ValueKind.String || right.Kind == ValueKind.String)
        {
            stack.Push(MbValue.String(left.ToDisplayString() + right.ToDisplayString()));
            return;
        }

        stack.Push(MbValue.Number(left.AsNumber() + right.AsNumber()));
    }

    private static void NumberBinary(Stack<MbValue> stack, Func<double, double, double> op)
    {
        double right = Pop(stack, OpCode.Add).AsNumber();
        double left = Pop(stack, OpCode.Add).AsNumber();
        stack.Push(MbValue.Number(op(left, right)));
    }

    private static void NumberCompare(Stack<MbValue> stack, Func<double, double, bool> op)
    {
        double right = Pop(stack, OpCode.Equal).AsNumber();
        double left = Pop(stack, OpCode.Equal).AsNumber();
        stack.Push(MbValue.Boolean(op(left, right)));
    }

    private static void Compare(Stack<MbValue> stack, Func<MbValue, MbValue, bool> op)
    {
        MbValue right = Pop(stack, OpCode.Equal);
        MbValue left = Pop(stack, OpCode.Equal);
        stack.Push(MbValue.Boolean(op(left, right)));
    }

    private void RegisterBuiltins()
    {
        RegisterHost("print", args =>
        {
            string text = string.Join(" ", args.Select(arg => arg.ToDisplayString()));
            _output.Add(text);
            _writer(text);
            return MbValue.Nil;
        });

        RegisterHost("type", args =>
        {
            if (args.Count != 1)
            {
                throw new MoonByteException($"type expects 1 arg, got {args.Count}");
            }

            return MbValue.String(args[0].Kind.ToString().ToLower(CultureInfo.InvariantCulture));
        });
    }
}


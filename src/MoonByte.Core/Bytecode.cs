namespace MoonByte.Core;

public enum OpCode
{
    Constant,
    Nil,
    True,
    False,
    GetGlobal,
    SetGlobal,
    GetLocal,
    SetLocal,
    Add,
    Subtract,
    Multiply,
    Divide,
    Negate,
    Not,
    Equal,
    NotEqual,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,
    Call,
    Return,
    Pop,
    MakeTable,
    GetProperty
}

public readonly record struct Instruction(OpCode OpCode, int Operand = 0, string? Name = null);

public sealed class BytecodeChunk
{
    private readonly List<Instruction> _instructions = [];
    private readonly List<MbValue> _constants = [];

    public IReadOnlyList<Instruction> Instructions => _instructions;

    public IReadOnlyList<MbValue> Constants => _constants;

    public int LocalCount { get; set; }

    public int AddConstant(MbValue value)
    {
        _constants.Add(value);
        return _constants.Count - 1;
    }

    public void Emit(OpCode opCode, int operand = 0, string? name = null) =>
        _instructions.Add(new Instruction(opCode, operand, name));
}


namespace MoonByte.Core;

public sealed class MoonByteEngine
{
    private readonly VirtualMachine _vm;

    public MoonByteEngine(Action<string>? output = null, int maxSteps = 100_000)
    {
        _vm = new VirtualMachine(output, maxSteps);
    }

    public IReadOnlyList<string> Output => _vm.Output;

    public void RegisterHost(string name, HostFunction function) => _vm.RegisterHost(name, function);

    public void SetGlobal(string name, MbValue value) => _vm.SetGlobal(name, value);

    public MbValue GetGlobal(string name) => _vm.GetGlobal(name);

    public RunResult Execute(string source)
    {
        BytecodeChunk chunk = Compile(source);
        MbValue value = _vm.Execute(chunk);
        return new RunResult(value, _vm.Output);
    }

    public BytecodeChunk Compile(string source)
    {
        var lexer = new Lexer(source);
        var parser = new Parser(lexer.Lex());
        var compiler = new Compiler();
        return compiler.Compile(parser.Parse());
    }
}

public sealed record RunResult(MbValue ReturnValue, IReadOnlyList<string> Output);


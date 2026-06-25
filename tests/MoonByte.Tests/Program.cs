using MoonByte.Core;

var tests = new (string Name, Action Body)[]
{
    ("arithmetic and variables", ArithmeticAndVariables),
    ("functions return values", FunctionsReturnValues),
    ("tables expose fields", TablesExposeFields),
    ("host functions can be embedded", HostFunctionsCanBeEmbedded),
    ("disassembler shows bytecode", DisassemblerShowsBytecode),
    ("runtime reports undefined globals", RuntimeReportsUndefinedGlobals)
};

int failed = 0;
foreach ((string name, Action body) in tests)
{
    try
    {
        body();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

if (failed > 0)
{
    Console.Error.WriteLine($"{failed} test(s) failed");
    return 1;
}

Console.WriteLine($"{tests.Length} test(s) passed");
return 0;

static void ArithmeticAndVariables()
{
    var engine = new MoonByteEngine();
    engine.Execute("""
    let x = 2 + 3 * 4;
    print(x);
    """);
    AssertEqual("14", engine.Output.Single());
}

static void FunctionsReturnValues()
{
    var engine = new MoonByteEngine();
    engine.Execute("""
    fn add(a, b) {
      return a + b;
    }
    print(add(7, 8));
    """);
    AssertEqual("15", engine.Output.Single());
}

static void TablesExposeFields()
{
    var engine = new MoonByteEngine();
    engine.Execute("""
    let player = { name: "bot", hp: 42 };
    print(player.name + ":" + player.hp);
    """);
    AssertEqual("bot:42", engine.Output.Single());
}

static void HostFunctionsCanBeEmbedded()
{
    int calls = 0;
    var engine = new MoonByteEngine();
    engine.RegisterHost("double", args =>
    {
        calls++;
        return MbValue.Number(args[0].AsNumber() * 2);
    });
    engine.Execute("print(double(9));");

    AssertEqual(1, calls);
    AssertEqual("18", engine.Output.Single());
}

static void DisassemblerShowsBytecode()
{
    var engine = new MoonByteEngine();
    string bytecode = Disassembler.Disassemble(engine.Compile("let x = 1 + 2;"));
    AssertContains("Constant", bytecode);
    AssertContains("Add", bytecode);
    AssertContains("SetGlobal x", bytecode);
}

static void RuntimeReportsUndefinedGlobals()
{
    var engine = new MoonByteEngine();
    try
    {
        engine.Execute("missing();");
    }
    catch (MoonByteException ex) when (ex.Message.Contains("undefined global", StringComparison.Ordinal))
    {
        return;
    }

    throw new InvalidOperationException("expected undefined global error");
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"expected {expected}, got {actual}");
    }
}

static void AssertContains(string needle, string haystack)
{
    if (!haystack.Contains(needle, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"expected output to contain '{needle}', got: {haystack}");
    }
}


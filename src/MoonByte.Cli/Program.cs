using MoonByte.Core;

if (args.Length == 0 || args[0] is "help" or "-h" or "--help")
{
    PrintHelp();
    return 0;
}

try
{
    return args[0] switch
    {
        "run" => RunFile(args),
        "disasm" => Disassemble(args),
        "bench" => Bench(args),
        "repl" => Repl(),
        "version" => Version(),
        _ => Fail($"unknown command '{args[0]}'")
    };
}
catch (MoonByteException ex)
{
    Console.Error.WriteLine($"moonbyte: {ex.Message}");
    return 1;
}

static int RunFile(string[] args)
{
    if (args.Length < 2)
    {
        return Fail("run requires a script path");
    }

    var engine = new MoonByteEngine(Console.WriteLine);
    engine.Execute(File.ReadAllText(args[1]));
    return 0;
}

static int Disassemble(string[] args)
{
    if (args.Length < 2)
    {
        return Fail("disasm requires a script path");
    }

    var engine = new MoonByteEngine();
    BytecodeChunk chunk = engine.Compile(File.ReadAllText(args[1]));
    Console.Write(Disassembler.Disassemble(chunk));
    return 0;
}

static int Bench(string[] args)
{
    if (args.Length < 2)
    {
        return Fail("bench requires a script path");
    }

    int iterations = args.Length >= 3 && int.TryParse(args[2], out int parsed) ? parsed : 100;
    string source = File.ReadAllText(args[1]);
    var engine = new MoonByteEngine();
    var watch = System.Diagnostics.Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
    {
        engine.Execute(source);
    }

    watch.Stop();
    Console.WriteLine($"iterations={iterations}");
    Console.WriteLine($"elapsed_ms={watch.Elapsed.TotalMilliseconds:0.###}");
    Console.WriteLine($"per_run_ms={watch.Elapsed.TotalMilliseconds / iterations:0.###}");
    return 0;
}

static int Repl()
{
    var engine = new MoonByteEngine(Console.WriteLine);
    Console.WriteLine("MoonByte REPL. Type .exit to quit.");
    while (true)
    {
        Console.Write("> ");
        string? line = Console.ReadLine();
        if (line is null || line.Trim() == ".exit")
        {
            return 0;
        }

        if (string.IsNullOrWhiteSpace(line))
        {
            continue;
        }

        try
        {
            engine.Execute(line);
        }
        catch (MoonByteException ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
    }
}

static int Version()
{
    Console.WriteLine("MoonByte VM 0.1.0");
    return 0;
}

static int Fail(string message)
{
    Console.Error.WriteLine($"moonbyte: {message}");
    PrintHelp();
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine("""
    MoonByte VM - tiny Lua-like bytecode interpreter

    Usage:
      moonbyte run <script.mb>
      moonbyte disasm <script.mb>
      moonbyte bench <script.mb> [iterations]
      moonbyte repl
      moonbyte version
    """);
}

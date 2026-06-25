using MoonByte.Core;

string scriptPath = args.Length > 0 ? args[0] : Path.Combine("examples", "scripts", "game_loop.mb");
var events = new List<string>();
var engine = new MoonByteEngine(line => events.Add($"script: {line}"));

engine.RegisterHost("spawn", hostArgs =>
{
    string name = hostArgs[0].AsString();
    double x = hostArgs[1].AsNumber();
    double y = hostArgs[2].AsNumber();
    events.Add($"host: spawn {name} at {x:0},{y:0}");
    return MbValue.Nil;
});

engine.RegisterHost("move", hostArgs =>
{
    string name = hostArgs[0].AsString();
    double dx = hostArgs[1].AsNumber();
    double dy = hostArgs[2].AsNumber();
    events.Add($"host: move {name} by {dx:0},{dy:0}");
    return MbValue.Nil;
});

for (int tick = 1; tick <= 3; tick++)
{
    engine.SetGlobal("tick", MbValue.Number(tick));
    engine.Execute(File.ReadAllText(scriptPath));
}

foreach (string line in events)
{
    Console.WriteLine(line);
}


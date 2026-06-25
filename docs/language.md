# Language Notes

MoonByte is Lua-like in spirit, not a Lua implementation. The current syntax is intentionally small and suitable for VM experiments.

## Values

- `number`
- `string`
- `boolean`
- `nil`
- `table`
- function
- host function

## Variables

```lua
let x = 10;
let name = "pilot";
```

Top-level `let` declarations become globals. Function-local `let` declarations become local slots.

## Functions

```lua
fn add(a, b) {
  return a + b;
}

print(add(2, 3));
```

## Tables

```lua
let player = { name: "pilot", hp: 100 };
print(player.name);
```

The MVP supports table literals and field reads. Mutation and dynamic indexing are intentionally left for later work.

## Host API

The host can register C# delegates:

```csharp
engine.RegisterHost("log", args =>
{
    Console.WriteLine(args[0].ToDisplayString());
    return MbValue.Nil;
});
```

Scripts can then call `log("hello")`.


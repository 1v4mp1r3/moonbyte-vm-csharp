using System.Globalization;

namespace MoonByte.Core;

public enum ValueKind
{
    Nil,
    Number,
    Boolean,
    String,
    Table,
    Function,
    HostFunction
}

public delegate MbValue HostFunction(IReadOnlyList<MbValue> args);

public sealed class MbValue : IEquatable<MbValue>
{
    public static readonly MbValue Nil = new(ValueKind.Nil, null);
    public static readonly MbValue True = new(ValueKind.Boolean, true);
    public static readonly MbValue False = new(ValueKind.Boolean, false);

    private MbValue(ValueKind kind, object? value)
    {
        Kind = kind;
        Value = value;
    }

    public ValueKind Kind { get; }

    public object? Value { get; }

    public static MbValue Number(double value) => new(ValueKind.Number, value);

    public static MbValue Boolean(bool value) => value ? True : False;

    public static MbValue String(string value) => new(ValueKind.String, value);

    public static MbValue Table(MbTable value) => new(ValueKind.Table, value);

    public static MbValue Function(MbFunction value) => new(ValueKind.Function, value);

    public static MbValue Host(HostFunction value) => new(ValueKind.HostFunction, value);

    public double AsNumber() => Kind == ValueKind.Number
        ? (double)Value!
        : throw new MoonByteException($"expected number, got {Kind.ToString().ToLowerInvariant()}");

    public string AsString() => Kind == ValueKind.String
        ? (string)Value!
        : throw new MoonByteException($"expected string, got {Kind.ToString().ToLowerInvariant()}");

    public MbTable AsTable() => Kind == ValueKind.Table
        ? (MbTable)Value!
        : throw new MoonByteException($"expected table, got {Kind.ToString().ToLowerInvariant()}");

    public MbFunction AsFunction() => Kind == ValueKind.Function
        ? (MbFunction)Value!
        : throw new MoonByteException($"expected function, got {Kind.ToString().ToLowerInvariant()}");

    public HostFunction AsHostFunction() => Kind == ValueKind.HostFunction
        ? (HostFunction)Value!
        : throw new MoonByteException($"expected host function, got {Kind.ToString().ToLowerInvariant()}");

    public bool IsTruthy() => Kind switch
    {
        ValueKind.Nil => false,
        ValueKind.Boolean => (bool)Value!,
        _ => true
    };

    public string ToDisplayString() => Kind switch
    {
        ValueKind.Nil => "nil",
        ValueKind.Number => ((double)Value!).ToString("0.##########", CultureInfo.InvariantCulture),
        ValueKind.Boolean => ((bool)Value!) ? "true" : "false",
        ValueKind.String => (string)Value!,
        ValueKind.Table => AsTable().ToDisplayString(),
        ValueKind.Function => $"<fn {AsFunction().Name}>",
        ValueKind.HostFunction => "<host-fn>",
        _ => ToString() ?? string.Empty
    };

    public bool Equals(MbValue? other)
    {
        if (other is null || Kind != other.Kind)
        {
            return false;
        }

        return Kind switch
        {
            ValueKind.Nil => true,
            ValueKind.Number => AsNumber().Equals(other.AsNumber()),
            ValueKind.Boolean => Equals(Value, other.Value),
            ValueKind.String => AsString() == other.AsString(),
            _ => ReferenceEquals(Value, other.Value)
        };
    }

    public override bool Equals(object? obj) => Equals(obj as MbValue);

    public override int GetHashCode() => HashCode.Combine(Kind, Value);

    public override string ToString() => ToDisplayString();
}

public sealed class MbTable
{
    private readonly Dictionary<string, MbValue> _fields = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, MbValue> Fields => _fields;

    public void Set(string name, MbValue value) => _fields[name] = value;

    public MbValue Get(string name) => _fields.TryGetValue(name, out MbValue? value) ? value : MbValue.Nil;

    public string ToDisplayString()
    {
        string fields = string.Join(", ", _fields.Select(pair => $"{pair.Key}: {pair.Value.ToDisplayString()}"));
        return $"{{{fields}}}";
    }
}

public sealed record MbFunction(string Name, IReadOnlyList<string> Parameters, BytecodeChunk Chunk);


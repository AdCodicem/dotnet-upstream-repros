using ProtoBuf;
using ProtoBuf.Meta;
using Shop;
using Shop.Values;

var model = RuntimeTypeModel.Create();

Console.WriteLine("== 1. A dictionary keyed by a type with a surrogate to a primitive");
var priced = new PricedBy { Stock = { [new Cents(250)] = 3 } };
var stream = new MemoryStream();
model.Serialize(stream, priced);
Console.WriteLine($"serializes: {Convert.ToHexString(stream.ToArray())}, round trip: {model.DeepClone(priced).Stock.Single()}");
Console.WriteLine("a member, a list and a dictionary value of it export:");
Console.WriteLine(Schema(model, typeof(PricedAs)));
Console.WriteLine("a dictionary key of it:");
try
{
    Schema(model, typeof(PricedBy));
}
catch (Exception e)
{
    Console.WriteLine($"GetSchema: {e.GetType().FullName}: {e.Message}");
    Console.WriteLine($" ---> {e.InnerException?.GetType().FullName}: {e.InnerException?.Message}");
    foreach (var frame in (e.InnerException?.StackTrace ?? "").Split('\n').Take(5)) Console.WriteLine("  " + frame.Trim().Split(" in ")[0]);
}

Console.WriteLine();
Console.WriteLine("== 2. The package guess counts a type served by a custom serializer, which the file never declares");
Console.WriteLine(Schema(model, typeof(Line)));
Console.WriteLine(Schema(model, typeof(PlainLine)));

Console.WriteLine("== 3. A root Guid on a Level300 model: the export and the bytes disagree");
var level300 = RuntimeTypeModel.Create();
level300.DefaultCompatibilityLevel = CompatibilityLevel.Level300;
var id = new Guid("00112233-4455-6677-8899-aabbccddeeff");
Console.WriteLine(Schema(level300, typeof(Guid)));
stream = new MemoryStream();
level300.Serialize(stream, id);
Console.WriteLine($"root Guid written:              {Convert.ToHexString(stream.ToArray())} (field 1 holds a .bcl.Guid, Level200)");
stream = new MemoryStream();
level300.Serialize(stream, new Holder { Id = id });
Console.WriteLine($"Holder {{ Guid Id = 1 }} written: {Convert.ToHexString(stream.ToArray())} (field 1 holds a string, Level300)");
// what the exported `message Guid { string value = 1; }` describes, as a peer writes it
var asExported = new MemoryStream();
level300.Serialize(asExported, new Holder { Id = id });
asExported.Position = 0;
try { Console.WriteLine($"root Guid read from the exported shape: {level300.Deserialize<Guid>(asExported)}"); }
catch (Exception e) { Console.WriteLine($"root Guid read from the exported shape: {e.GetType().Name}: {e.Message.Split(';')[0]}"); }
foreach (var (name, value) in new (string, object)[] { ("DateTime", new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)), ("TimeSpan", TimeSpan.FromMinutes(90)), ("decimal", 1.5m) })
{
    var exported = Schema(level300, value.GetType()).Split('\n').First(l => l.Contains("value = 1")).Trim();
    Console.WriteLine($"root {name}: exported at Level300 as `{exported}`; written at Level200 {Hex(RuntimeTypeModel.Create(), value)}, at Level300 {Hex(level300, value)}");
}

static string Schema(TypeModel model, Type type) => model.GetSchema(new SchemaGenerationOptions { Types = { type }, Syntax = ProtoSyntax.Proto3 });
static string Hex(TypeModel model, object value) { var stream = new MemoryStream(); model.Serialize(stream, value); return Convert.ToHexString(stream.ToArray()); }

namespace Shop
{
    [ProtoContract]
    public class PricedBy
    {
        [ProtoMember(1)] public Dictionary<Shop.Values.Cents, int> Stock { get; set; } = [];
    }

    [ProtoContract]
    public class PricedAs
    {
        [ProtoMember(1)] public Shop.Values.Cents Single { get; set; }
        [ProtoMember(2)] public List<Shop.Values.Cents> Many { get; set; } = [];
        [ProtoMember(3)] public Dictionary<string, Shop.Values.Cents> ByName { get; set; } = [];
    }

    [ProtoContract]
    public class Line
    {
        [ProtoMember(1)] public string? Product { get; set; }
        [ProtoMember(2)] public Shop.Values.Quantity Quantity { get; set; }
    }

    [ProtoContract]
    public class PlainLine
    {
        [ProtoMember(1)] public string? Product { get; set; }
        [ProtoMember(2)] public int Quantity { get; set; }
    }

    [ProtoContract]
    public class Holder
    {
        [ProtoMember(1)] public Guid Id { get; set; }
    }
}

namespace Shop.Values
{
    /// <summary>Crosses the wire as an int64, through a surrogate.</summary>
    [ProtoContract(Surrogate = typeof(long))]
    public readonly record struct Cents(long Value)
    {
        public static implicit operator long(Cents value) => value.Value;
        public static implicit operator Cents(long value) => new(value);
    }

    /// <summary>Crosses the wire as an int32, through a scalar serializer.</summary>
    [ProtoContract(Serializer = typeof(QuantitySerializer), Name = "int32")]
    public readonly record struct Quantity(int Value);

    public sealed class QuantitySerializer : ProtoBuf.Serializers.ISerializer<Quantity>
    {
        public ProtoBuf.Serializers.SerializerFeatures Features
            => ProtoBuf.Serializers.SerializerFeatures.CategoryScalar | ProtoBuf.Serializers.SerializerFeatures.WireTypeVarint;
        public Quantity Read(ref ProtoReader.State state, Quantity value) => new(state.ReadInt32());
        public void Write(ref ProtoWriter.State state, Quantity value) => state.WriteInt32(value.Value);
    }
}

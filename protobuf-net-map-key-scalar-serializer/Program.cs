using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

var model = RuntimeTypeModel.Create();

Console.WriteLine("== 1. The same bytes, a map for a string key, a list of entries for a Sku key");
Console.WriteLine($"Dictionary<string, int>: {Hex(new ByString { Stock = { ["a"] = 1 } })}");
Console.WriteLine($"Dictionary<Sku, int>:    {Hex(new BySku { Stock = { [new Sku("a")] = 1 } })}");
Console.WriteLine(Schema(typeof(ByString)));
Console.WriteLine(Schema(typeof(BySku)));

Console.WriteLine("== 2. A key sent twice: the last wins for the map, the entry list throws");
// field 1, two entries for the key "a": the values 1, then 9
var twice = Convert.FromHexString("0A050A01611001" + "0A050A01611009");
Console.WriteLine($"Dictionary<string, int>: {Try(() => model.Deserialize<ByString>(new MemoryStream(twice)).Stock["a"].ToString())}");
Console.WriteLine($"Dictionary<Sku, int>:    {Try(() => model.Deserialize<BySku>(new MemoryStream(twice)).Stock[new Sku("a")].ToString())}");

Console.WriteLine();
Console.WriteLine("== 3. Two such dictionaries over two key types named alike export one entry message name twice");
Console.WriteLine(Schema(typeof(Catalogue)));
Directory.CreateDirectory("obj/proto"); // obj/: ignored by git
File.WriteAllText("obj/proto/catalogue.proto", Schema(typeof(Catalogue)));

string Hex(object value) { var stream = new MemoryStream(); model.Serialize(stream, value); return Convert.ToHexString(stream.ToArray()); }
string Schema(Type type) => model.GetSchema(new SchemaGenerationOptions { Types = { type }, Syntax = ProtoSyntax.Proto3 });
static string Try(Func<string> read) { try { return read(); } catch (Exception e) { return $"{e.GetType().Name}: {e.Message}"; } }

/// <summary>A string that crosses the wire as a string, through a scalar serializer.</summary>
[ProtoContract(Serializer = typeof(SkuSerializer), Name = "string")]
public readonly record struct Sku(string Value);

/// <summary>Another one.</summary>
[ProtoContract(Serializer = typeof(EanSerializer), Name = "string")]
public readonly record struct Ean(string Value);

public sealed class SkuSerializer : ISerializer<Sku>
{
    public SerializerFeatures Features => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeString;
    public Sku Read(ref ProtoReader.State state, Sku value) => new(state.ReadString());
    public void Write(ref ProtoWriter.State state, Sku value) => state.WriteString(value.Value);
}

public sealed class EanSerializer : ISerializer<Ean>
{
    public SerializerFeatures Features => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeString;
    public Ean Read(ref ProtoReader.State state, Ean value) => new(state.ReadString());
    public void Write(ref ProtoWriter.State state, Ean value) => state.WriteString(value.Value);
}

[ProtoContract] public class ByString { [ProtoMember(1)] public Dictionary<string, int> Stock { get; set; } = []; }
[ProtoContract] public class BySku { [ProtoMember(1)] public Dictionary<Sku, int> Stock { get; set; } = []; }

[ProtoContract]
public class Catalogue
{
    [ProtoMember(1)] public Dictionary<Sku, int> BySku { get; set; } = [];
    [ProtoMember(2)] public Dictionary<Ean, int> ByEan { get; set; } = [];
}

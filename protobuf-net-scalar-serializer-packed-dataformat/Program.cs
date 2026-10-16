using Google.Protobuf;
using Google.Protobuf.Collections;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

var model = RuntimeTypeModel.Create();

Console.WriteLine("== 1. A list of a CategoryScalar | WireTypeVarint type is never packed");
var basket = new Basket
{
    Ints = [1, 2, 3], Quantities = [new(1), new(2), new(3)],
    PackedInts = [1, 2, 3], PackedQuantities = [new(1), new(2), new(3)],
};
Console.WriteLine($"written: {Hex(Write(basket))}");
Console.WriteLine("  field 3, List<int>, IsPacked = true:      1A03010203 (packed)");
Console.WriteLine("  field 4, List<Quantity>, IsPacked = true: 200120022003 (not packed)");

Console.WriteLine();
Console.WriteLine("== 2. ...and packed data for it cannot be read");
// What a proto3 peer sends for `repeated int32 ... = 1` and `= 2`: packed, the proto3 default.
var peerInts = Peer(output => new RepeatedField<int> { 1, 2, 3 }.WriteTo(output, FieldCodec.ForInt32(WireFormat.MakeTag(1, WireFormat.WireType.LengthDelimited))));
var peerQuantities = Peer(output => new RepeatedField<int> { 1, 2, 3 }.WriteTo(output, FieldCodec.ForInt32(WireFormat.MakeTag(2, WireFormat.WireType.LengthDelimited))));
Console.WriteLine($"peer bytes for field 1: {Hex(peerInts)}, read as List<int>: {Try(() => string.Join(",", Read<Basket>(peerInts).Ints))}");
Console.WriteLine($"peer bytes for field 2: {Hex(peerQuantities)}, read as List<Quantity>: {Try(() => string.Join(",", Read<Basket>(peerQuantities).Quantities.Select(q => q.Value)))}");

Console.WriteLine();
Console.WriteLine("== 3. The proto3 export marks the List<int> [packed = false], not the List<Quantity>");
Console.WriteLine(model.GetSchema(new SchemaGenerationOptions { Types = { typeof(Basket) }, Syntax = ProtoSyntax.Proto3 }));

Console.WriteLine("== 4. A member's DataFormat never reaches a CategoryScalar serializer");
Console.WriteLine($"written, Plain = -3 and Custom = -3, both DataFormat.ZigZag: {Hex(Write(new Delta { Plain = -3, Custom = new(-3) }))}");
// What a peer sends for `sint32 Plain = 1; sint32 Custom = 2;` holding -3 each.
var peerDelta = Peer(output => { output.WriteTag(1, WireFormat.WireType.Varint); output.WriteSInt32(-3); output.WriteTag(2, WireFormat.WireType.Varint); output.WriteSInt32(-3); });
var delta = Read<Delta>(peerDelta);
Console.WriteLine($"peer bytes for sint32 -3, -3: {Hex(peerDelta)}, read back: Plain = {delta.Plain}, Custom = {delta.Custom.Value}");
Console.WriteLine(model.GetSchema(new SchemaGenerationOptions { Types = { typeof(Delta) }, Syntax = ProtoSyntax.Proto3 }));

byte[] Write<T>(T value) { var stream = new MemoryStream(); model.Serialize(stream, value); return stream.ToArray(); }
T Read<T>(byte[] bytes) => model.Deserialize<T>(new MemoryStream(bytes));
static byte[] Peer(Action<CodedOutputStream> write) { var stream = new MemoryStream(); var output = new CodedOutputStream(stream); write(output); output.Flush(); return stream.ToArray(); }
static string Hex(byte[] bytes) => Convert.ToHexString(bytes);
static string Try(Func<string> read) { try { return read(); } catch (Exception e) { return $"{e.GetType().Name}: {e.Message}"; } }

/// <summary>An int that crosses the wire as an int32, through a scalar serializer.</summary>
[ProtoContract(Serializer = typeof(QuantitySerializer), Name = "int32")]
public readonly record struct Quantity(int Value);

public sealed class QuantitySerializer : ISerializer<Quantity>
{
    public SerializerFeatures Features => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
    public Quantity Read(ref ProtoReader.State state, Quantity value) => new(state.ReadInt32());
    public void Write(ref ProtoWriter.State state, Quantity value) => state.WriteInt32(value.Value);
}

[ProtoContract]
public class Basket
{
    [ProtoMember(1)] public List<int> Ints { get; set; } = [];
    [ProtoMember(2)] public List<Quantity> Quantities { get; set; } = [];
    [ProtoMember(3, IsPacked = true)] public List<int> PackedInts { get; set; } = [];
    [ProtoMember(4, IsPacked = true)] public List<Quantity> PackedQuantities { get; set; } = [];
}

[ProtoContract]
public class Delta
{
    [ProtoMember(1, DataFormat = DataFormat.ZigZag)] public int Plain { get; set; }
    [ProtoMember(2, DataFormat = DataFormat.ZigZag)] public Quantity Custom { get; set; }
}

using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

var order = new Order { Single = new(3), Many = [new(1), new(2)] };
#if NULLABLE
order.Maybe = new(4);
#endif
#if MAP_VALUE
order.ByName["a"] = new(5);
#endif
#if MAP_KEY
order.ByQuantity[new(6)] = "b";
#endif
Console.WriteLine($"runtime model:   {Hex(RuntimeTypeModel.Create(), order)}");
Console.WriteLine($"generated model: {Hex(AppModel.Instance, order)}");

static string Hex(TypeModel model, Order value) { var stream = new MemoryStream(); model.Serialize(stream, value); return Convert.ToHexString(stream.ToArray()); }

/// <summary>An int that crosses the wire as an int32, through a scalar serializer.</summary>
[ProtoContract(Serializer = typeof(QuantitySerializer))]
public readonly record struct Quantity(int Value);

public sealed class QuantitySerializer : ISerializer<Quantity>
{
    public SerializerFeatures Features => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
    public Quantity Read(ref ProtoReader.State state, Quantity value) => new(state.ReadInt32());
    public void Write(ref ProtoWriter.State state, Quantity value) => state.WriteInt32(value.Value);
}

[ProtoContract]
public class Order
{
    [ProtoMember(1)] public Quantity Single { get; set; }
    [ProtoMember(2)] public List<Quantity> Many { get; set; } = [];
#if NULLABLE
    [ProtoMember(3)] public Quantity? Maybe { get; set; }
#endif
#if MAP_VALUE
    [ProtoMember(4)] public Dictionary<string, Quantity> ByName { get; set; } = [];
#endif
#if MAP_KEY
    [ProtoMember(5)] public Dictionary<Quantity, string> ByQuantity { get; set; } = [];
#endif
}

[ProtoModel]
[ProtoSerializable(typeof(Order))]
public partial class AppModel : TypeModel;

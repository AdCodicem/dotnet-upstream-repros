using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

var stream = new MemoryStream();
AppModel.Instance.Serialize(stream, new Order { Single = new(3), Many = [new(1), new(2)] });
Console.WriteLine($"dynamic code: {System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}, written: {Convert.ToHexString(stream.ToArray())}");
stream.Position = 0;
var read = AppModel.Instance.Deserialize<Order>(stream);
Console.WriteLine($"read back: Single = {read.Single.Value}, Many = {string.Join(",", read.Many.Select(q => q.Value))}");

/// <summary>An int that crosses the wire as an int32, through a scalar serializer.</summary>
[ProtoContract(Serializer = typeof(QuantitySerializer))]
#if MEMBERS
[TypeConverter(typeof(Quantity.Converter))]
#endif
public readonly record struct Quantity(int Value)
{
#if MEMBERS
    // Two ordinary members of a domain type, which protobuf-net never touches: a JSON helper...
    [RequiresUnreferencedCode("Reflection-based JSON.")]
    [RequiresDynamicCode("Reflection-based JSON.")]
    public static Quantity FromJson(string json) => JsonSerializer.Deserialize<Quantity>(json);

    // ...and the nested TypeConverter its [TypeConverter] names.
    public sealed class Converter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string);
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => new Quantity(int.Parse((string)value, culture));
    }
#endif
}

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
}

[ProtoModel]
[ProtoSerializable(typeof(Order))]
public partial class AppModel : TypeModel;

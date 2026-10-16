using Akka.Actor;
using Akka.Serialization;
using Akka.Serialization.V2;
using MessagePack;
using Repro;

var setup = ReproSerializer.CreateRegistration().CreateSetup();
var bootstrap = BootstrapSetup.Create().WithConfig("akka.loglevel = WARNING");
using var system = ActorSystem.Create("repro", bootstrap.And(setup));

var message = new PlaceOrder(new Sku("ABC-123"), Quantity: 2);
var serializer = system.Serialization.FindSerializerFor(message);
var bytes = serializer.ToBinary(message);
var manifest = Akka.Serialization.Serialization.ManifestFor(serializer, message);
var back = (PlaceOrder)system.Serialization.Deserialize(bytes, serializer.Identifier, manifest);

Console.WriteLine($"Serializer: {serializer.GetType().Name} ({serializer.Identifier}), manifest \"{manifest}\", {bytes.Length} bytes");
Console.WriteLine($"Round trip: {back} -> equal: {back == message}");

namespace Repro
{
    public interface IReproProtocol;

    // A type the generator cannot annotate (it stands for any type from another library): a single string.
    public readonly record struct Sku(string Value) : IStringValue<Sku>
    {
        public static Sku From(string value) => new(value);
    }

    public interface IStringValue<TSelf> where TSelf : IStringValue<TSelf>
    {
        string Value { get; }
        static abstract TSelf From(string value);
    }

    // One formatter written once for every such type.
    public class StringValueFormatter<T> : IAkkaMessagePackFormatter<T> where T : IStringValue<T>
    {
        public void Write(ref MessagePackWriter writer, T value) => writer.Write(value.Value);
        public T Read(ref MessagePackReader reader) => T.From(reader.ReadString()!);
        public int SizeOf(T value) => SerializerV2.UnknownSize;
    }

    [AkkaSerializable(Manifest = "place-order-v1")]
    public sealed record PlaceOrder(
        [property: AkkaField(0)] Sku Sku,
        [property: AkkaField(1)] int Quantity) : IReproProtocol;

#if WORKAROUND
    // Workaround: a closed, non-generic subclass per target type.
    public sealed class SkuFormatter : StringValueFormatter<Sku>;

    [AkkaSerializer<IReproProtocol>("repro", 120900)]
    [AkkaSerializerFormatter<Sku, SkuFormatter>]
    public sealed partial class ReproSerializer : AkkaSerializer
    {
        public static partial SerializerRegistration CreateRegistration();
    }
#else
    [AkkaSerializer<IReproProtocol>("repro", 120900)]
    [AkkaSerializerFormatter<Sku, StringValueFormatter<Sku>>]
    public sealed partial class ReproSerializer : AkkaSerializer
    {
        public static partial SerializerRegistration CreateRegistration();
    }
#endif
}

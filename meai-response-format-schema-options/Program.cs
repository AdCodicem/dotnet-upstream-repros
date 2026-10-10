using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

// The schema options an application would like to use everywhere: describe Iban, which its JsonConverter writes as a
// string, with the constraints that converter enforces.
var schemaOptions = new AIJsonSchemaCreateOptions { TransformSchemaNode = DescribeIban };

// 1. Tools: AIFunctionFactory takes the options.
Print("AIFunctionFactory.Create(PlaceOrder), default options",
    AIFunctionFactory.Create(Tools.PlaceOrder).JsonSchema);
Print("AIFunctionFactory.Create(PlaceOrder), JsonSchemaCreateOptions = schemaOptions",
    AIFunctionFactory.Create(Tools.PlaceOrder, new AIFunctionFactoryOptions { JsonSchemaCreateOptions = schemaOptions }).JsonSchema);

// 2. Structured output: no way to pass them.
Print("ChatResponseFormat.ForJsonSchema<Order>()", ChatResponseFormat.ForJsonSchema<Order>().Schema!.Value);

// 3. GetResponseAsync<T> replaces a ResponseFormat the caller set, so the schema cannot be supplied that way either.
var callerFormat = ChatResponseFormat.ForJsonSchema(
    AIJsonUtilities.CreateJsonSchema(typeof(Order), serializerOptions: AIJsonUtilities.DefaultOptions,
        inferenceOptions: schemaOptions with { IncludeSchemaKeyword = true }),
    "Order");
var client = new ScriptedChatClient("""{"account":"FR7630006000011234567890189","quantity":3,"alternates":["DE89370400440532013000"]}""");
ChatResponse<Order> typed = await client.GetResponseAsync<Order>(
    "Place an order.", new ChatOptions { ResponseFormat = callerFormat });
Print("GetResponseAsync<Order>(..., new ChatOptions { ResponseFormat = callerFormat }): format the IChatClient receives",
    client.LastFormat!.Schema!.Value);
Console.WriteLine($"  is it callerFormat? {ReferenceEquals(client.LastFormat, callerFormat)}; Result.Account = {typed.Result.Account}");
Console.WriteLine();

// 4. What the application wants GetResponseAsync<T> to send: the same transform, through AIJsonUtilities.
Print("Wanted: AIJsonUtilities.CreateJsonSchema(typeof(Order), ..., inferenceOptions: schemaOptions with { IncludeSchemaKeyword = true })",
    callerFormat.Schema!.Value);

// 5. Without the schema, the rule is only enforced after the model has answered.
var refusing = new ScriptedChatClient("""{"account":"FR76","quantity":3,"alternates":[]}""");
ChatResponse<Order> refused = await refusing.GetResponseAsync<Order>("Place an order.");
try
{
    _ = refused.Result;
}
catch (JsonException e)
{
    Console.WriteLine($"Answer with \"account\":\"FR76\", .Result: {e.GetType().Name}: {e.Message}");
    Console.WriteLine();
}

// 6. Today's workaround: build the format by hand, call the non-generic GetResponseAsync, wrap the response.
var workaround = new ScriptedChatClient("""{"account":"FR7630006000011234567890189","quantity":3,"alternates":[]}""");
ChatResponse raw = await workaround.GetResponseAsync("Place an order.", new ChatOptions { ResponseFormat = callerFormat });
var wrapped = new ChatResponse<Order>(raw, AIJsonUtilities.DefaultOptions);
Console.WriteLine($"Workaround (non-generic GetResponseAsync + new ChatResponse<Order>): format received is callerFormat? " +
    $"{ReferenceEquals(workaround.LastFormat, callerFormat)}; Result.Account = {wrapped.Result.Account}");
Console.WriteLine();

// 7. Data annotations on the properties: no "type", repeated on every property, and they describe the list, not its items.
Print("ChatResponseFormat.ForJsonSchema<AnnotatedOrder>() ([StringLength] and [RegularExpression] on both Iban properties)",
    ChatResponseFormat.ForJsonSchema<AnnotatedOrder>().Schema!.Value);

foreach (Type attribute in new[] { typeof(StringLengthAttribute), typeof(RegularExpressionAttribute) })
{
    Console.WriteLine($"{attribute.Name} targets: {attribute.GetCustomAttribute<AttributeUsageAttribute>()!.ValidOn}");
}

static JsonNode DescribeIban(AIJsonSchemaCreateContext context, JsonNode schema)
{
    // The schema of a type with a custom converter is `true` (or an object holding only a description).
    if (context.TypeInfo.Type == typeof(Iban))
    {
        return Describe(schema as JsonObject ?? []);
    }

    // System.Text.Json's exporter does not call the transform for a collection element whose schema is `true`, so the
    // collection's own node completes its `items`.
    if (context.TypeInfo.ElementType == typeof(Iban) && schema is JsonObject collection)
    {
        if (collection["items"] is JsonObject items)
        {
            Describe(items);
        }
        else
        {
            collection["items"] = Describe([]);
        }
    }

    return schema;

    static JsonObject Describe(JsonObject node)
    {
        node["type"] = "string";
        node["minLength"] = 15;
        node["maxLength"] = 34;
        node["pattern"] = "^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$";
        return node;
    }
}

static void Print(string title, JsonElement schema)
{
    Console.WriteLine(title);
    Console.WriteLine("  " + JsonSerializer.Serialize(schema));
    Console.WriteLine();
}

/// <summary>A value object: serialized as a JSON string by its own converter, which enforces its rules.</summary>
[JsonConverter(typeof(IbanJsonConverter))]
public readonly struct Iban(string value)
{
    public string Value { get; } = value;

    public override string ToString() => Value;
}

public sealed class IbanJsonConverter : JsonConverter<Iban>
{
    public override Iban Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("An IBAN is a JSON string.");
        }

        string text = reader.GetString()!;
        if (text.Length is < 15 or > 34)
        {
            throw new JsonException("An IBAN has 15 to 34 characters.");
        }

        return new Iban(text);
    }

    public override void Write(Utf8JsonWriter writer, Iban value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value);
}

public sealed record Order(Iban Account, int Quantity, List<Iban> Alternates);

public sealed record AnnotatedOrder(
    [property: StringLength(34, MinimumLength = 15), RegularExpression("^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$")] Iban Account,
    int Quantity,
    [property: StringLength(34, MinimumLength = 15), RegularExpression("^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$")] List<Iban> Alternates);

public static class Tools
{
    public static string PlaceOrder(Iban account, int quantity) => $"placed {quantity} on {account}";
}

/// <summary>Stands for the model: records the response format it is sent, answers with a fixed text.</summary>
public sealed class ScriptedChatClient(string answer) : IChatClient
{
    public ChatResponseFormatJson? LastFormat { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        LastFormat = options?.ResponseFormat as ChatResponseFormatJson;
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

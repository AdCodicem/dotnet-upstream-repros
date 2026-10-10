using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

const string Answer = """{"account":"FR7630006000011234567890189","quantity":3,"alternates":[]}""";
JsonSerializerOptions serializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions; // what RunAsync<T> uses by default

// The format the application would like RunAsync<T> to send: Iban described with the rules its converter enforces.
var schemaOptions = new AIJsonSchemaCreateOptions { IncludeSchemaKeyword = true, TransformSchemaNode = DescribeIban };
ChatResponseFormatJson wanted = ChatResponseFormat.ForJsonSchema(
    AIJsonUtilities.CreateJsonSchema(typeof(Order), serializerOptions: serializerOptions, inferenceOptions: schemaOptions),
    "Order");

// 1. RunAsync<T>: the format the IChatClient receives.
var client = new ScriptedChatClient(Answer);
AIAgent agent = client.AsAIAgent(instructions: "You place orders.");
AgentResponse<Order> typed = await agent.RunAsync<Order>("Place an order.");
Print("agent.RunAsync<Order>(...): format the IChatClient receives", client.LastFormat!.Schema!.Value);
Console.WriteLine($"  Result.Account = {typed.Result.Account}");
Console.WriteLine();

// 2. A ResponseFormat the caller sets on AgentRunOptions is replaced by RunAsync<T>.
typed = await agent.RunAsync<Order>("Place an order.", options: new AgentRunOptions { ResponseFormat = wanted });
Print("agent.RunAsync<Order>(..., options: new AgentRunOptions { ResponseFormat = wanted }): format the IChatClient receives",
    client.LastFormat!.Schema!.Value);
Console.WriteLine($"  is it the caller's format? {ReferenceEquals(client.LastFormat, wanted)}");
Console.WriteLine();

// 3. What the application wants sent.
Print("Wanted: AIJsonUtilities.CreateJsonSchema(typeof(Order), ..., inferenceOptions: schemaOptions)", wanted.Schema!.Value);

// 4. Today's workaround: the non-generic RunAsync with the format, then AgentResponse<T> by hand.
AgentResponse raw = await agent.RunAsync("Place an order.", options: new AgentRunOptions { ResponseFormat = wanted });
var wrapped = new AgentResponse<Order>(raw, serializerOptions);
Console.WriteLine($"Workaround (non-generic RunAsync + new AgentResponse<Order>): format received is the caller's? " +
    $"{ReferenceEquals(client.LastFormat, wanted)}; Result.Account = {wrapped.Result.Account}");

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

using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// 0. What the parameter types' converters say about the refused values.
foreach (var (title, read) in new (string, Action)[]
{
    ("JsonSerializer.Deserialize<Quantity>(\"500\")", () => JsonSerializer.Deserialize<Quantity>("500")),
    ("JsonSerializer.Deserialize<Iban>(\"\\\"FR76\\\"\")", () => JsonSerializer.Deserialize<Iban>("\"FR76\"")),
})
{
    try
    {
        read();
    }
    catch (JsonException e)
    {
        Console.WriteLine($"{title}: {e.GetType().Name}: {e.Message}");
    }
}

Console.WriteLine();

// 1. Automatic function calling through the OpenAI connector; a fake endpoint stands for the model and scripts the call.
foreach (bool retainArgumentTypes in new[] { false, true })
{
    Console.WriteLine($"== OpenAI connector, FunctionChoiceBehavior.Auto(), RetainArgumentTypes = {retainArgumentTypes}");
    foreach (var (function, arguments) in new[]
    {
        ("SetQuantity", """{"quantity":500}"""),
        ("SetQuantity", """{"quantity":3}"""),
        ("SetAccount", """{"account":"FR76"}"""),
        ("SetAccount", """{"account":"FR7630006000011234567890189"}"""),
        ("SetCheckedQuantity", """{"quantity":500}"""),
    })
    {
        var endpoint = new FakeOpenAIEndpoint("orders-" + function, arguments);
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion("gpt-4o", "test-key", httpClient: new HttpClient(endpoint));
        builder.Plugins.AddFromType<OrderPlugin>("orders");
        Kernel kernel = builder.Build();

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(
                options: new FunctionChoiceBehaviorOptions { RetainArgumentTypes = retainArgumentTypes }),
        };
        await kernel.InvokePromptAsync("Place an order.", new KernelArguments(settings));

        Console.WriteLine($"{function}({arguments})");
        Console.WriteLine($"  tool message sent back to the model: {endpoint.ToolMessage}");
    }

    Console.WriteLine();
}

// 2. The same function invoked directly, with each argument shape the connector produces.
{
    Kernel kernel = new();
    KernelFunction setQuantity = kernel.CreatePluginFromType<OrderPlugin>("orders")["SetQuantity"];
    foreach (var (title, value) in new (string, object)[]
    {
        ("JsonElement 500 (what RetainArgumentTypes = true hands over)", JsonDocument.Parse("500").RootElement),
        ("string \"500\" (what RetainArgumentTypes = false hands over)", "500"),
    })
    {
        try
        {
            await setQuantity.InvokeAsync(kernel, new KernelArguments { ["quantity"] = value });
        }
        catch (Exception e)
        {
            Console.WriteLine($"SetQuantity.InvokeAsync, quantity = {title}:");
            Console.WriteLine($"  {e.GetType().FullName}: {e.Message}");
            foreach (string frame in e.StackTrace!.Split('\n').Take(4))
            {
                Console.WriteLine("  " + frame.Trim());
            }
        }
    }
}

public sealed class OrderPlugin
{
    [KernelFunction, Description("Sets the quantity ordered.")]
    public string SetQuantity(Quantity quantity) => $"quantity set to {quantity}";

    [KernelFunction, Description("Sets the account to debit.")]
    public string SetAccount(Iban account) => $"account set to {account}";

    [KernelFunction, Description("Sets the quantity ordered (a type that also has a TypeConverter).")]
    public string SetCheckedQuantity(CheckedQuantity quantity) => $"quantity set to {quantity}";
}

/// <summary>A value object over an int: a JSON number, refused outside 1 to 100 by its converter.</summary>
[JsonConverter(typeof(QuantityJsonConverter))]
public readonly struct Quantity(int value)
{
    public int Value { get; } = value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public sealed class QuantityJsonConverter : JsonConverter<Quantity>
{
    public override Quantity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int value))
        {
            throw new JsonException("A quantity is a whole JSON number.");
        }

        if (value is < 1 or > 100)
        {
            throw new JsonException("A quantity is between 1 and 100.");
        }

        return new Quantity(value);
    }

    public override void Write(Utf8JsonWriter writer, Quantity value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Value);
}

/// <summary>A value object over a string: a JSON string of 15 to 34 characters.</summary>
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

/// <summary>The same as <see cref="Quantity"/>, with a TypeConverter too.</summary>
[JsonConverter(typeof(CheckedQuantityJsonConverter))]
[TypeConverter(typeof(CheckedQuantityTypeConverter))]
public readonly struct CheckedQuantity(int value)
{
    public int Value { get; } = value;

    public static CheckedQuantity Create(int value)
        => value is < 1 or > 100 ? throw new ArgumentException("A quantity is between 1 and 100.") : new(value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public sealed class CheckedQuantityJsonConverter : JsonConverter<CheckedQuantity>
{
    public override CheckedQuantity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int value))
        {
            throw new JsonException("A quantity is a whole JSON number.");
        }

        try
        {
            return CheckedQuantity.Create(value);
        }
        catch (ArgumentException e)
        {
            throw new JsonException(e.Message, e);
        }
    }

    public override void Write(Utf8JsonWriter writer, CheckedQuantity value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Value);
}

public sealed class CheckedQuantityTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => CheckedQuantity.Create(int.Parse((string)value, NumberStyles.Integer, CultureInfo.InvariantCulture));
}

/// <summary>
/// Stands for the OpenAI chat completions endpoint: the first call answers with a call to the plugin's function, the
/// second records the tool message the connector sends back and ends the conversation.
/// </summary>
public sealed class FakeOpenAIEndpoint(string functionName, string functionArguments) : HttpMessageHandler
{
    private int _calls;

    public string? ToolMessage { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using JsonDocument body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
        foreach (JsonElement message in body.RootElement.GetProperty("messages").EnumerateArray())
        {
            if (message.GetProperty("role").GetString() == "tool")
            {
                ToolMessage = message.GetProperty("content").ToString();
            }
        }

        string json = _calls++ == 0
            ? """
              {"id":"c1","object":"chat.completion","created":1700000000,"model":"gpt-4o",
               "choices":[{"index":0,"finish_reason":"tool_calls","message":{"role":"assistant","content":null,
                 "tool_calls":[{"id":"call_1","type":"function","function":{"name":NAME,"arguments":ARGUMENTS}}]}}],
               "usage":{"prompt_tokens":1,"completion_tokens":1,"total_tokens":2}}
              """
                .Replace("NAME", JsonSerializer.Serialize(functionName))
                .Replace("ARGUMENTS", JsonSerializer.Serialize(functionArguments))
            : """
              {"id":"c2","object":"chat.completion","created":1700000000,"model":"gpt-4o",
               "choices":[{"index":0,"finish_reason":"stop","message":{"role":"assistant","content":"Done."}}],
               "usage":{"prompt_tokens":1,"completion_tokens":1,"total_tokens":2}}
              """;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}

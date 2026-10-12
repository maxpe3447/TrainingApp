using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TrainingApp.Api.Common;
using TrainingApp.Api.Features.ExpressDelivery;
using TrainingApp.Api.Features.PickupDelivery;
using TrainingApp.Api.Features.StandardDelivery;

namespace TrainingApp.Checks;

/// <summary>
/// The student's delivery feature, found by reflection: the checks work whatever the feature is called.
/// </summary>
public sealed class StudentFeature
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Type[] BuiltInStrategies =
        [typeof(StandardDeliveryStrategy), typeof(ExpressDeliveryStrategy), typeof(PickupDeliveryStrategy)];

    private StudentFeature(Type strategyType, Type commandType)
    {
        StrategyType = strategyType;
        CommandType = commandType;
    }

    /// <summary>Your class that implements <see cref="IDeliveryStrategy{TCommand}"/>.</summary>
    public Type StrategyType { get; }

    /// <summary>The command your strategy handles.</summary>
    public Type CommandType { get; }

    /// <summary>Namespace of your feature folder, e.g. TrainingApp.Api.Features.SameDayDelivery.</summary>
    public string Namespace => StrategyType.Namespace ?? string.Empty;

    /// <summary>Returns the student's feature, or fails the check with instructions.</summary>
    public static StudentFeature Find()
    {
        var candidates = typeof(IDeliveryCommand).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && !BuiltInStrategies.Contains(type))
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDeliveryStrategy<>))
                .Select(i => new StudentFeature(type, i.GenericTypeArguments[0])))
            .ToList();

        candidates.Should().NotBeEmpty(
            "task 01 asks you to add your own IDeliveryStrategy<TCommand> implementation " +
            "in src/TrainingApp.Api/Features/<YourFeature>/");

        candidates.Should().HaveCount(1,
            "the checks follow a single feature of your own, but found: {0}",
            string.Join(", ", candidates.Select(c => c.StrategyType.Name)));

        return candidates[0];
    }

    /// <summary>Resolves your strategy from the application's DI container.</summary>
    public DeliveryType GetDeliveryType(IServiceProvider services)
    {
        var serviceType = typeof(IDeliveryStrategy<>).MakeGenericType(CommandType);
        var strategy = services.GetService(serviceType);

        strategy.Should().NotBeNull("{0} must be registered as {1}; AddDeliveryStrategies does it for every strategy in the API project",
            StrategyType.Name, serviceType.Name);

        return (DeliveryType)serviceType.GetProperty(nameof(IDeliveryStrategy<IDeliveryCommand>.Type))!.GetValue(strategy)!;
    }

    /// <summary>Posts <see cref="MyFeature.ValidRequest"/> to <see cref="MyFeature.Route"/> and returns the quote.</summary>
    public static async Task<DeliveryQuote> PostValidRequestAsync(HttpClient client, CancellationToken cancellationToken)
    {
        MyFeature.Route.Should().NotBeNullOrWhiteSpace("fill in Route in tests/TrainingApp.Checks/MyFeature.cs");
        MyFeature.ValidRequest.Should().NotBeNull("fill in ValidRequest in tests/TrainingApp.Checks/MyFeature.cs");

        using var response = await client.PostAsJsonAsync(MyFeature.Route, MyFeature.ValidRequest, JsonOptions, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        response.IsSuccessStatusCode.Should().BeTrue(
            "POST {0} with MyFeature.ValidRequest must succeed, but it answered {1}: {2}",
            MyFeature.Route, (int)response.StatusCode, body);

        return JsonSerializer.Deserialize<DeliveryQuote>(body, JsonOptions)!;
    }
}

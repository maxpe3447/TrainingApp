namespace TrainingApp.Checks;

/// <summary>
/// Task 05: tests of your feature on three levels. See tasks/05-testing.md.
/// These checks only look for your tests in the source code; whether they pass is what `dotnet test` itself tells you.
/// </summary>
[Trait("Task", "05")]
public sealed class Task05TestingChecks
{
    private static readonly string[] TestAttributes = ["[Fact", "[Theory"];
    private static readonly string[] TelemetryTools = ["FakeLogger", "FakeLogCollector", "MetricCollector", "ActivityListener"];

    [Fact]
    public void Unit_tests_cover_your_feature_with_mocks()
    {
        var feature = StudentFeature.Find();

        var files = TestFilesMentioning("TrainingApp.UnitTests", feature.StrategyType.Name, feature.CommandType.Name);

        files.Should().Contain(file => file.Contains("Mock<", StringComparison.Ordinal),
            "add unit tests for your feature to tests/TrainingApp.UnitTests/Features/<YourFeature>/: " +
            "test one method at a time and replace its dependencies with Moq mocks, see ExpressDeliveryHandlerTests");
    }

    [Fact]
    public void Integration_tests_cover_your_feature()
    {
        var feature = StudentFeature.Find();

        var files = TestFilesMentioning("TrainingApp.IntegrationTests", feature.CommandType.Name);

        files.Should().NotBeEmpty(
            "add integration tests that send {0} through the real pipeline (ISender or the in-memory HttpClient), " +
            "see DeliveryPipelineTests", feature.CommandType.Name);
    }

    [Fact]
    public void Feature_telemetry_is_tested()
    {
        var feature = StudentFeature.Find();

        var files = TestFilesMentioning("TrainingApp.UnitTests", feature.StrategyType.Name, feature.CommandType.Name)
            .Concat(TestFilesMentioning("TrainingApp.IntegrationTests", feature.StrategyType.Name, feature.CommandType.Name));

        files.Should().Contain(file => TelemetryTools.Any(tool => file.Contains(tool, StringComparison.Ordinal)),
            "test the logs, metrics or spans of your feature from steps 02-04 with FakeLogger, MetricCollector " +
            "or ActivityListener, see PickupDeliveryStrategyTests and DeliveryTelemetryTests");
    }

    [Fact]
    public void E2E_tests_call_your_endpoint()
    {
        MyFeature.Route.Should().NotBeNullOrWhiteSpace("fill in Route in tests/TrainingApp.Checks/MyFeature.cs");

        var files = TestFilesMentioning("TrainingApp.E2ETests", MyFeature.Route!);

        files.Should().NotBeEmpty(
            "add e2e tests that call {0} on the API container, see DeliveryEndpointsTests", MyFeature.Route);
    }

    /// <summary>Contents of the test source files of a project that mention any of the given words.</summary>
    private static IReadOnlyList<string> TestFilesMentioning(string project, params string[] words)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "tests", project);
        Directory.Exists(directory).Should().BeTrue("the project tests/{0} comes with the 05-testing branch", project);

        return Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(File.ReadAllText)
            .Where(text => TestAttributes.Any(attribute => text.Contains(attribute, StringComparison.Ordinal))
                && words.Any(word => text.Contains(word, StringComparison.Ordinal)))
            .ToList();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TrainingApp.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the checks run from inside the repository");
        return directory!.FullName;
    }
}

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;

namespace TrainingApp.E2ETests;

/// <summary>
/// Builds the API image from src/TrainingApp.Api/Dockerfile, starts a container and gives tests an HttpClient for it.
/// xUnit calls InitializeAsync before the first test of the collection and DisposeAsync after the last one;
/// Testcontainers removes the container and the image afterwards (and its Ryuk sidecar cleans up if the run crashes).
/// </summary>
public sealed class ApiContainerFixture : IAsyncLifetime
{
    private const ushort ApiPort = 8080;

    private IFutureDockerImage? _image;
    private IContainer? _container;

    public HttpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _image = new ImageFromDockerfileBuilder()
            // The build context is the repository root: the Dockerfile needs Directory.*.props from there.
            .WithDockerfileDirectory(new CommonDirectoryPath(FindRepositoryRoot()), string.Empty)
            .WithDockerfile("src/TrainingApp.Api/Dockerfile")
            .WithCleanUp(true)
            .Build();
        await _image.CreateAsync();

        _container = new ContainerBuilder(_image)
            // Random free host port: tests can run in parallel with a local `dotnet run` on 5161.
            .WithPortBinding(ApiPort, assignRandomHostPort: true)
            // Production, like a real deployment: no Scalar/OpenAPI, JSON console logs.
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Production")
            // Start the tests only when the app answers HTTP, not when the process merely started.
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request.ForPort(ApiPort).ForPath("/metrics")))
            .Build();
        await _container.StartAsync();

        Client = new HttpClient
        {
            BaseAddress = new UriBuilder(Uri.UriSchemeHttp, _container.Hostname, _container.GetMappedPublicPort(ApiPort)).Uri
        };
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        if (_image is not null)
        {
            await _image.DisposeAsync();
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TrainingApp.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("TrainingApp.slnx not found above " + AppContext.BaseDirectory);
    }
}

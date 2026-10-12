namespace TrainingApp.IntegrationTests;

/// <summary>
/// A collection fixture: ONE <see cref="TrainingAppFactory"/> is created for all test classes marked
/// [Collection(IntegrationCollection.Name)] and disposed after the last of them. Starting the app once is much faster
/// than once per class (IClassFixture) or per test (constructor). Tests of one collection run one at a time,
/// so they can safely clear and read the shared log collector.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<TrainingAppFactory>
{
    public const string Name = "Integration";
}

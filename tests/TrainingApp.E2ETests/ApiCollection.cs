namespace TrainingApp.E2ETests;

/// <summary>
/// Building the image and starting the container takes tens of seconds: do it once for every test class
/// marked [Collection(ApiCollection.Name)].
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiContainerFixture>
{
    public const string Name = "API container";
}

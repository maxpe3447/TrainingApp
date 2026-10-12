# Step 05 — Testing: prove that your feature works, on three levels

## What you will learn

- **Unit tests**: one method of one class; every dependency replaced with a **Moq** mock or a fake.
- **Integration tests**: the real DI container, MediatR pipeline and HTTP stack in memory (`WebApplicationFactory`),
  with only the boundaries replaced (clock, log sink).
- **End-to-end tests**: the API built into a Docker image and started with **Testcontainers**; tests use HTTP only.
- **xUnit fixtures**: sharing expensive setup between tests (`IClassFixture`, `ICollectionFixture`, `IAsyncLifetime`).
- **FluentAssertions**: readable assertions with precise failure messages.
- **Testing telemetry**: `FakeLogger`, `MetricCollector`, `ActivityListener`.

## The three levels

|                      | Unit                               | Integration                          | End-to-end                          |
|----------------------|------------------------------------|--------------------------------------|-------------------------------------|
| Project              | `tests/TrainingApp.UnitTests`      | `tests/TrainingApp.IntegrationTests` | `tests/TrainingApp.E2ETests`        |
| What runs            | one method                         | the app in memory                    | the app in a Docker container       |
| Dependencies         | Moq mocks, fakes                   | real, except clock and logs          | everything real                     |
| Talks to the app via | method calls                       | `ISender` or in-memory `HttpClient`  | HTTP over the network               |
| Speed                | milliseconds                       | ~1 s to start the app                | ~30–60 s to build and start the image |
| A failure tells you  | exactly which rule is broken       | which pieces do not fit together     | that a client would see a problem   |

Read the examples in this order:

1. `UnitTests/Features/StandardDelivery/StandardDeliveryStrategyTests.cs` — no dependencies; `[Fact]` and `[Theory]`.
2. `UnitTests/Features/ExpressDelivery/ExpressDeliveryHandlerTests.cs` — **Moq**: `Setup`/`Returns`, `Verify`,
   `It.IsAny`, `It.Is`, `Times`; `FakeTimeProvider` for "today".
3. `UnitTests/Features/ExpressDelivery/ExpressDeliveryValidatorTests.cs` — mocking a collaborator's answer;
   FluentValidation `TestValidate`.
4. `UnitTests/Behaviors/ValidationBehaviorTests.cs` and `MetricsBehaviorTests.cs` — mocking a delegate and an interface;
   matching arguments with a predicate.
5. `UnitTests/Features/PickupDelivery/PickupDeliveryStrategyTests.cs` — testing **logs** with `FakeLogger`.
6. `UnitTests/Features/ExpressDelivery/ExpressDeliveryStrategyTests.cs` — testing **metrics** with `MetricCollector`.
7. `IntegrationTests/TrainingAppFactory.cs` and `IntegrationCollection.cs` — a custom `WebApplicationFactory`
   shared through a **collection fixture**; `ConfigureTestServices` replaces the clock.
8. `IntegrationTests/DeliveryPipelineTests.cs`, `DeliveryApiTests.cs`, `DeliveryTelemetryTests.cs` — pipeline, HTTP,
   and **spans** with `ActivityListener`.
9. `E2ETests/ApiContainerFixture.cs` — `IAsyncLifetime` + Testcontainers: image from `src/TrainingApp.Api/Dockerfile`,
   random port, wait strategy; `ApiCollection.cs`, `DeliveryRequests.cs` (`[MemberData]`), `DeliveryEndpointsTests.cs`.

## Run the tests

```bash
dotnet test tests/TrainingApp.UnitTests
dotnet test tests/TrainingApp.IntegrationTests
dotnet test tests/TrainingApp.E2ETests          # Docker must be running
```

## Your task

Cover **your** feature on all three levels. Put the files in a folder named after your feature in each project
(e.g. `tests/TrainingApp.UnitTests/Features/<YourFeature>/`): these files are yours, so later course updates will not conflict.

1. **Unit tests** (`TrainingApp.UnitTests`):
   - your strategy: pricing, limits (use `[Theory]` for the boundaries), arrival date;
   - your validator, with the strategy mocked: field rules, and that the business rule asks `CanDeliver`;
   - your handler, if it has logic of its own, with the strategy mocked and `FakeTimeProvider`;
   - at least one test with `Mock<…>` and `Verify`.
2. **Telemetry tests**: the logs of step 02 with `FakeLogger`, the metrics of step 03 with `MetricCollector`,
   or the spans of step 04 with `ActivityListener` — at least one of them, all three for the bonus.
3. **Integration tests** (`TrainingApp.IntegrationTests`): send your command through `ISender` and through the
   in-memory `HttpClient`; assert the exact quote using the fake "today"; check a rejected command.
4. **E2E tests** (`TrainingApp.E2ETests`): a valid request to your endpoint returns `200` with your delivery type,
   invalid requests return `400` (`[MemberData]` with a data class like `DeliveryRequests`).

Rules of thumb:

- One behavior per test; the name says the scenario and the expected result.
- Arrange / Act / Assert, separated by blank lines.
- Mock what you do not own in that test; do not mock the class under test, and do not mock value objects.
- Assert results first; verify calls only when the call itself is the behavior (e.g. "next is not called").

## Check yourself

```bash
dotnet test tests/TrainingApp.Checks --filter Task=05
dotnet test                                      # everything, including your new tests
```

## Questions to think about

- Which of your bugs would only an e2e test catch? Which would it catch but explain badly?
- `MetricsBehaviorTests` mocks `IAppMetrics`, but `ExpressDeliveryStrategyTests` uses a real `IMeterFactory`. Why?
- Why is the clock replaced in integration tests but not in e2e tests? What do the e2e tests do about dates instead?
- Why does `ApiContainerFixture` wait for `/metrics` to answer, instead of waiting for the container to start?

## That's the course

You built a feature, made it observable with logs, metrics and traces, and covered it with tests.
Ideas to continue: call another service over HTTP and follow one trace across both; add an alert rule to Prometheus;
add an OpenTelemetry Collector processor that drops health-check spans.

# Step 02 — Logging: make your feature tell what it decides

## What you will learn

- **Structured logging**: message templates whose placeholders become separate, queryable attributes.
- Log **levels** and how to pick one; filtering levels per category and per provider in `appsettings.json`.
- High-performance logging with the `[LoggerMessage]` source generator.
- Shipping logs with **OpenTelemetry**: app → OTLP → otel-collector → **Loki** → **Grafana**.

## What changed in this step

| File                                              | What to look at                                                          |
|---------------------------------------------------|--------------------------------------------------------------------------|
| `Program.cs`                                      | Two logging providers: Serilog (console) and OpenTelemetry (OTLP)        |
| `appsettings*.json`                               | Levels per category and per provider (`Logging:Serilog:LogLevel`); OTLP endpoint |
| `Behaviors/LoggingBehavior.cs`                    | One start and one end record for every command — no code in features needed |
| `Features/PickupDelivery/PickupDeliveryStrategy.cs` | A strategy that logs **why** it refuses a parcel                       |
| `Features/PickupDelivery/PickupDeliveryLog.cs`    | `[LoggerMessage]`: log methods generated at compile time                 |
| `Features/StandardDelivery/StandardDeliveryHandler.cs` | Plain `ILogger` calls with templates; Information vs Debug          |
| `monitoring/`                                     | Docker stack: otel-collector, Loki, Grafana with a logs dashboard        |

## Run it

```bash
docker compose -f monitoring/docker-compose.yml up -d
dotnet run --project src/TrainingApp.Api
```

Send a few requests from `TrainingApp.Api.http` (include the invalid ones), then:

1. Look at the console: readable lines in Development. Run with `--environment Production` and compare: JSON lines,
   and only warnings and errors (`Logging:Serilog:LogLevel` in `appsettings.json`).
2. Open Grafana at <http://localhost:3000> → dashboard **TrainingApp — Logs**. Expand a log line:
   `Command`, `WeightKg`, `PickupPointId`… are separate attributes, not just text.
3. Open **Explore** → Loki and try a few queries:

   ```logql
   {service_name="TrainingApp.Api"} | detected_level="warn"
   {service_name="TrainingApp.Api"} | PickupPointId="ODESA-99"
   sum by (Command) (count_over_time({service_name="TrainingApp.Api"} | Command != "" [5m]))
   ```

## Your task

The `LoggingBehavior` already logs that your command started, finished or was rejected.
It cannot know **why** your feature made its decisions — that is your job now.

In **your feature folder only**:

1. Inject `ILogger<T>` into your strategy and/or handler (strategies are DI singletons, so constructor injection works).
2. Log the business decisions of your feature. For example:
   - why `CanDeliver` refuses a parcel (which rule failed, with the values);
   - which surcharges or discounts were applied to the price;
   - how the arrival date was calculated.
3. Use message templates with placeholders, never string interpolation. Name placeholders in PascalCase
   and keep the same name for the same thing everywhere (`WeightKg`, not `Weight` in one place and `Kg` in another).
4. Choose levels deliberately: `Debug` for details that help while developing, `Information` for business events,
   `Warning` only for something that needs attention. A normal successful quote must not log warnings.
5. Write at least one log message with `[LoggerMessage]`, like `PickupDeliveryLog`.

Bonus:

- Wrap the work of your handler in `logger.BeginScope(...)` and find the scope attributes in Loki.
- Build a Grafana panel that counts how often your feature refuses parcels, grouped by reason.

## Check yourself

```bash
dotnet test tests/TrainingApp.Checks --filter Task=02
```

Then look at your feature's logs in Grafana: can you tell from the logs alone why a given request was refused?

## Questions to think about

- Why `logger.LogInformation("Parcel {WeightKg}", w)` and not `logger.LogInformation($"Parcel {w}")`?
  Think about Loki, memory allocations, and disabled levels.
- What must never be logged? (Personal data, secrets, full request bodies…) How would you protect against it?
- Production shows only warnings in the console, but Loki receives information records. Why could that be a good setup?

## Next step

Commit your work and merge `03-metrics`:

```bash
git merge origin/03-metrics
```

# Step 04 — Tracing: follow one request through your feature

## What you will learn

- **Traces and spans**: a trace is the tree of spans of one request; in .NET a span is an `Activity`.
- `ActivitySource`, span **attributes**, **events**, **status** and **exceptions**.
- **Context propagation**: the W3C `traceparent` header, and how a child span finds its parent (`Activity.Current`).
- **Correlating the three signals**: logs carry the `trace_id`, metric **exemplars** point to traces,
  and Grafana jumps between Loki, Prometheus and Tempo.

## What changed in this step

| File                                              | What to look at                                                        |
|---------------------------------------------------|------------------------------------------------------------------------|
| `Program.cs`                                      | `WithTracing`: sources, ASP.NET Core instrumentation, OTLP exporter; exemplars |
| `Telemetry/AppTelemetry.cs`                       | The application's `ActivitySource`                                     |
| `Behaviors/TracingBehavior.cs`                    | A span per command; rejected vs failed; why it is registered first     |
| `Common/DeliveryQuoteHandler.cs`                  | Child spans for every step of the quote algorithm                      |
| `Features/StandardDelivery/StandardDeliveryHandler.cs` | A span of its own with business attributes and an event           |
| `monitoring/`                                     | Tempo added; otel-collector routes traces; datasources link the signals; dashboard **TrainingApp — Logs & Traces** |

## Run it

```bash
docker compose -f monitoring/docker-compose.yml up -d
dotnet run --project src/TrainingApp.Api
```

Send requests from `TrainingApp.Api.http`, then in Grafana <http://localhost:3000>:

1. **TrainingApp — Logs & Traces** → click a trace id in *Recent traces*. Look at the tree:
   HTTP request span → `StandardDeliveryCommand` → `StandardDeliveryCommand.quote` → `…calculate_cost`.
   Open a span: attributes, the `fragile_surcharge_applied` event.
2. In the same dashboard, the logs of that trace are listed below it. In the console, the `TraceId` at the end
   of each line is the same id.
3. **Logs → traces**: in a log line, expand it and click *Open trace*.
4. **Metrics → traces**: on **TrainingApp — Metrics**, the duration panel shows exemplars (dots);
   hover one and open its trace.
5. **Explore** → Tempo, TraceQL:

   ```traceql
   { resource.service.name = "TrainingApp.Api" && span.outcome = "rejected" }
   { span.parcel.fragile = true && duration > 1ms }
   ```

## Your task

Your command already has spans: the command span from `TracingBehavior` and, if your handler derives from
`DeliveryQuoteHandler`, the `calculate_cost` / `estimate_arrival` steps. They show **that** time was spent,
not **what** your feature was doing. In **your feature folder only**:

1. Start at least one span of your own with `AppTelemetry.ActivitySource.StartActivity("<YourCommand>.<step>")`
   inside your handler or strategy, always in a `using`.
   Good candidates: a lookup (a zone, a tariff, a country), a rule evaluation, a calculation with several parts.
2. Add **attributes** that describe the input and the result of that step (`parcel.weight_kg`, `zone.id`, `tariff.name`).
   Use lowercase dotted names. Unlike metric tags, attributes may have many distinct values.
3. Add at least one **event** for a notable moment: a surcharge applied, a fallback taken, a limit reached.
4. When your strategy refuses a parcel, record it as an attribute or event — **not** as an error status:
   a refusal is a valid answer, not a failure of the service.
5. Always write `activity?.` — `StartActivity` returns `null` when nobody listens.

Bonus:

- Simulate a slow dependency (`await Task.Delay(...)` in a span of your own for some inputs) and find the slow traces
  in *Failed or slow traces*, then via an exemplar on the duration panel.
- Throw an exception for a special input and look at how the span, the log record and the `outcome="error"` metric
  show the same failure.

## Check yourself

```bash
dotnet test tests/TrainingApp.Checks --filter Task=04
```

## Questions to think about

- Your feature now writes a log record, a metric and a span about the same refusal. When do you look at which?
- Why does `TracingBehavior` run before `LoggingBehavior` and `MetricsBehavior`? What breaks if it runs last?
- Why is a parcel weight fine as a span attribute but not as a metric tag?
- What would you need to keep one trace across two services? (Hint: `traceparent`, and what the checks send.)

## Next step

Commit your work and merge `05-testing`:

```bash
git merge origin/05-testing
```

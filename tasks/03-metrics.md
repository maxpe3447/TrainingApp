# Step 03 — Metrics: measure what your feature does

## What you will learn

- The **instrument types** of `System.Diagnostics.Metrics`: counter, up-down counter, histogram — and when to use each.
- **Tags** (dimensions) and **cardinality**: why `reason="too_heavy"` is fine and `customer_id="42"` is not.
- `IMeterFactory` and why instruments are created once.
- **OpenTelemetry metrics** exported to **Prometheus** (pull model) and queried with **PromQL** in **Grafana**.
- The difference between **technical** metrics (rate, errors, duration — the same for every command)
  and **business** metrics (only your feature knows them).

## What changed in this step

| File                                              | What to look at                                                         |
|---------------------------------------------------|-------------------------------------------------------------------------|
| `Program.cs`                                      | `WithMetrics`: which meters are exported; `MapPrometheusScrapingEndpoint` |
| `Telemetry/IAppMetrics.cs`, `AppMetrics.cs`       | Technical metrics per command: counter, in-progress, duration, value    |
| `Telemetry/MetricTimer.cs`, `InProgressScope.cs`  | `using`-scoped helpers built on `IDisposable` structs                   |
| `Behaviors/MetricsBehavior.cs`                    | Records the technical metrics for every command, with an `outcome` tag  |
| `Features/ExpressDelivery/ExpressDeliveryStrategy.cs` | **Business metrics**: refusals by reason, declared value histogram |
| `monitoring/`                                     | Prometheus added; dashboard **TrainingApp — Metrics**                   |

## Run it

```bash
docker compose -f monitoring/docker-compose.yml up -d
dotnet run --project src/TrainingApp.Api
```

1. Open <http://localhost:5161/metrics>: this is what Prometheus scrapes every 5 seconds.
   Find `standard_delivery_command_total` and `express_delivery_refusals_total`.
2. Send requests from `TrainingApp.Api.http`, including express parcels over 10 kg and 1000 km.
3. Open Grafana <http://localhost:3000> → **TrainingApp — Metrics**. Your command already shows up in the
   technical panels — `MetricsBehavior` did that, not you.
4. Open **Explore** → Prometheus and try:

   ```promql
   sum by (outcome) (rate(express_delivery_command_total[1m]))
   sum by (reason) (increase(express_delivery_refusals_total[5m]))
   histogram_quantile(0.95, sum by (le) (rate(express_delivery_command_duration_seconds_bucket[1m])))
   ```

## Your task

Technical metrics of your command are already there. Now add metrics that describe **your business**,
in **your feature folder only**:

1. Inject `IMeterFactory` into your strategy (or handler) and create your instruments **once**, in the constructor,
   on `meterFactory.Create(AppTelemetry.MeterName)` — only that meter is exported.
2. Add at least two business metrics of different types. Ideas:
   - a **counter** of refusals by reason (`too_heavy`, `too_far`, `storm`…);
   - a **histogram** of a business value: weight, distance, volume, declared value — with bucket boundaries that fit it;
   - a **counter** of applied surcharges or discounts by kind.
3. Give every metric at least one tag with a small, fixed set of values. Never use ids, raw weights or free text as tags.
4. Name metrics in lowercase `snake_case`, prefixed with your feature (`same_day_delivery_…`),
   set `unit` and `description`. Do not add `_total`: the exporter adds it to counters.
5. Build a Grafana panel for your metrics, export the dashboard as JSON
   (**Share → Export → Save to file**) and save it as `monitoring/grafana/dashboards/<your-feature>.json` —
   a file of your own, so it never conflicts with the course dashboards.

Bonus:

- An alert-worthy query: "more than half of the requests to my feature are refused in the last 5 minutes".
- Use `ObservableGauge` for something that is read on demand rather than recorded on an event.

## Check yourself

```bash
dotnet test tests/TrainingApp.Checks --filter Task=03
```

## Questions to think about

- Your feature now has logs **and** metrics about refusals. What can a metric answer that the logs cannot, and vice versa?
- Why is the request duration a histogram and not an average? What does p95 tell you that the average hides?
- What happens to Prometheus if someone tags a metric with the parcel weight?
- Why `IMeterFactory` instead of `static readonly Meter Meter = new("...")`? Hint: look at how the checks find your metric.

## Next step

Commit your work and merge `04-tracing`:

```bash
git merge origin/04-tracing
```

# TrainingApp — a hands-on course in .NET telemetry

TrainingApp is a small ASP.NET Core API that quotes parcel delivery prices.
Over five steps you extend it with your own feature and then make that feature observable
with **logs**, **metrics** and **traces**, and finally cover it with **tests**.

The course is built so that you keep working on **your own code** the whole time:
every step adds new infrastructure and examples, and you apply them to the feature you wrote in the first step.

## Course map

Every step is a git branch. Each branch contains everything from the previous one plus the new topic.

| Branch        | Topic                                                                  | Your task                                                   |
|---------------|------------------------------------------------------------------------|-------------------------------------------------------------|
| `main`        | Course description (you are here)                                      | —                                                           |
| `01-strategy` | Strategy pattern, MediatR pipeline, FluentValidation                   | Add your own delivery strategy and its endpoint             |
| `02-logging`  | Structured logging, Serilog, OpenTelemetry logs → Loki → Grafana       | Log the business decisions of your feature                  |
| `03-metrics`  | OpenTelemetry metrics → Prometheus → Grafana                           | Add business metrics to your feature and a dashboard panel  |
| `04-tracing`  | OpenTelemetry traces → Tempo; linking logs, metrics and traces         | Add custom spans, attributes and events to your feature     |
| `05-testing`  | Unit tests (Moq), integration tests, e2e tests (Testcontainers)        | Test your feature on all three levels                       |

The task of each step lives in [`tasks/`](tasks) of its branch, e.g. `tasks/01-strategy.md`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/get-docker/) — for the monitoring stack (from `02-logging`) and e2e tests (`05-testing`)
- An IDE: Visual Studio, Rider or VS Code with C# Dev Kit
- Basic git: branches, commit, merge

## How to work through the course

### 1. Start from the first step on your own branch

```bash
git clone <this repository>
cd <repository folder>
git switch -c my-work origin/01-strategy
```

All your work goes to `my-work` (any name is fine). Never commit to the course branches themselves.

### 2. Do the task

Open `tasks/01-strategy.md` and follow it. Commit as often as you like:

```bash
git add -A
git commit -m "Task 01: same-day delivery"
```

### 3. Check yourself

Every step ships automatic checks for its task:

```bash
dotnet test tests/TrainingApp.Checks --filter Task=01
```

The checks are red until you complete the task — that is expected. Their failure messages tell you what is missing.
`--filter "Task=01|Task=02"` runs several steps at once; without a filter all checks run.

### 4. Move on to the next step, keeping your code

```bash
git fetch origin
git merge origin/02-logging
```

The merge brings in the new infrastructure, examples, task and checks; your feature stays as it is.
Then open `tasks/02-logging.md` and continue — now you add logging **to the code you already wrote**.

## Rules that keep merges conflict-free

A merge conflicts only when you and the course changed the same file. The course is designed so that this does not happen,
as long as you follow these rules:

1. **Keep your feature in its own folder:** `src/TrainingApp.Api/Features/<YourFeature>/` — command, strategy, validator,
   handler, controller and your own `.http` file.
2. **Shared files you are allowed to edit** — the course branches never change them after `01-strategy`:
   - `src/TrainingApp.Api/Common/DeliveryType.cs` — append your delivery type;
   - `tests/TrainingApp.Checks/MyFeature.cs` — tell the checks how to call your endpoint.
3. **Everything else belongs to the course:** `Program.cs`, `Common/`, `Behaviors/`, `Telemetry/`, built-in features,
   `monitoring/`, `tests/`, project files. Read them, learn from them, but do not edit them.
   Strategies, handlers, validators and controllers are discovered automatically, so you never need to touch `Program.cs`.

If a merge does conflict anyway:

```bash
git merge --abort                      # go back to where you were, or
git checkout --theirs <course file>    # take the course version of a file you were not supposed to edit
```

## Got lost?

Each step is self-contained: you can always start a step from scratch with the reference features only:

```bash
git switch -c my-work-03 origin/03-metrics
```

The checks of that step expect a feature of your own, so do task 01 again first (it takes a few minutes the second time).

## For maintainers

See [CONTRIBUTING.md](CONTRIBUTING.md): branch order, how to propagate fixes and how to add a step.

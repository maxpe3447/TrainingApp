# Step 01 — Strategy pattern: add your own delivery type

## What you will learn

- The **Strategy pattern**: one algorithm (making a quote) that works with interchangeable strategies.
- A **MediatR** request pipeline: controller → `ISender` → pipeline behaviors → handler.
- Request validation with **FluentValidation** and returning validation errors as `ProblemDetails`.
- Vertical slices: everything a feature needs lives in one folder.

## Get to know the code

Start the API and play with it:

```bash
dotnet run --project src/TrainingApp.Api
```

Open <http://localhost:5161/scalar/v1> or send the requests from `src/TrainingApp.Api/TrainingApp.Api.http`.

Then read the code in this order:

1. `Common/IDeliveryStrategy.cs` — the strategy contract. The strategy is typed by its command,
   so dependency injection picks the implementation by the command type.
2. `Common/DeliveryQuoteHandler.cs` — the strategy pattern **context**: the quote algorithm is written once
   and talks to strategies only through the interface.
3. `Features/ExpressDelivery/` — a complete feature: command, strategy, validator, handler.
   Note how the validator reuses `strategy.CanDeliver`.
4. `Features/StandardDelivery/StandardDeliveryHandler.cs` — a handler that does not use the base class.
5. `Common/DeliveryStrategyRegistration.cs` and `Program.cs` — why a new feature needs no registration:
   strategies, handlers and validators are discovered in the assembly.
6. `Behaviors/ValidationBehavior.cs` and `Infrastructure/ValidationExceptionHandler.cs` — how a validation failure
   becomes a `400 Bad Request` with `application/problem+json`.

## Your task

Invent a delivery type of your own and add it as a new feature. Some ideas:

| Idea                | Command fields                                   | Rules                                          |
|---------------------|--------------------------------------------------|------------------------------------------------|
| Same-day delivery   | weight, distance, `bool IsBeforeNoon`            | ≤ 5 kg, ≤ 50 km, arrives today or tomorrow     |
| Cargo delivery      | weight, volume, distance, `bool NeedsLoader`     | 30–1000 kg, price per m³, loader surcharge     |
| International       | weight, country code, declared value             | list of supported countries, customs fee       |
| Drone delivery      | weight, distance, `string Weather`               | ≤ 2 kg, ≤ 10 km, no delivery in a storm        |

Requirements:

1. Create a folder `src/TrainingApp.Api/Features/<YourFeature>/` and put **all** your code there:
   - `<YourFeature>Command` — a record implementing `IDeliveryCommand`;
   - `<YourFeature>Strategy` — implements `IDeliveryStrategy<YourCommand>` with your own pricing and arrival rules;
   - `<YourFeature>Validator` — validates the fields and uses `strategy.CanDeliver` for the business rule;
   - `<YourFeature>Handler` — derives from `DeliveryQuoteHandler<YourCommand>`;
   - `<YourFeature>Controller` — a controller with `[Route("api/delivery")]` and one `[HttpPost("<your-route>")]` action
     (copy the attributes from `Controllers/DeliveryController.cs`);
   - `<YourFeature>.http` — requests for your endpoint: a valid one and a few invalid ones.
2. Append a value for your delivery type to `Common/DeliveryType.cs`.
3. Fill in `tests/TrainingApp.Checks/MyFeature.cs`: your route, a valid request and an invalid request.

Do not change `Program.cs` or any other file outside your feature folder, except the two files above.
This is what lets you merge the next steps without conflicts (see the README).

## Check yourself

```bash
dotnet test tests/TrainingApp.Checks --filter Task=01
```

All checks must be green. Then try your endpoint by hand: the valid request returns a quote,
the invalid ones return `400` with a list of errors.

## Questions to think about

- What would you need to change if strategies were chosen by a `DeliveryType` value in the request
  instead of by the command type? What would you gain and lose?
- Why does the validator call `CanDeliver`, instead of the strategy throwing an exception?
- Where is the right place for a rule like "no deliveries on Sundays": the validator, the strategy or the handler?

## Next step

Commit your work and merge `02-logging`:

```bash
git merge origin/02-logging
```

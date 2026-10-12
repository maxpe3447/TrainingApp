# Maintaining the course

## Branch chain

The course branches form one linear chain. Every branch is the previous branch plus one topic:

```
main ─► 01-strategy ─► 02-logging ─► 03-metrics ─► 04-tracing ─► 05-testing
```

Students merge the next branch into their own work branch, so the chain must always hold:
**every branch contains all commits of the branch before it.**

## Branch update order

1. Commit a fix to the **earliest** branch where the affected code exists.
   A typo in `README.md` goes to `main`; a bug in `DeliveryQuoteHandler` goes to `01-strategy`;
   a fix in the Loki config goes to `02-logging`.
2. Propagate it down the chain by merging each branch into the next one, in order:

   ```powershell
   ./scripts/sync-branches.ps1          # Windows / PowerShell
   ./scripts/sync-branches.ps1 -Push    # and push all course branches
   ```

   ```bash
   ./scripts/sync-branches.sh           # Linux / macOS / Git Bash
   ./scripts/sync-branches.sh --push
   ```

   The script stops at the first conflict. Resolve it, commit the merge, and run the script again.
3. Build every branch you touched (`dotnet build`) before pushing.

Rules:

- **Never rebase, amend or force-push a published course branch.** Students merge these branches;
  rewritten history makes their merges replay the whole course as conflicts.
- Never cherry-pick a fix to a later branch instead of merging: the same change would come twice through the chain.
- Pull all course branches before running the script: it works on local branches only.

## Student-owned files

Students edit these files, so after `01-strategy` no branch may change them — a change would conflict with every student:

- `src/TrainingApp.Api/Common/DeliveryType.cs`
- `src/TrainingApp.Api/Controllers/DeliveryController.cs`
- `src/TrainingApp.Api/TrainingApp.Api.http`
- `tests/TrainingApp.Checks/MyFeature.cs`

Students also own everything under `src/TrainingApp.Api/Features/` that is not a built-in feature
(`StandardDelivery`, `ExpressDelivery`, `PickupDelivery`). From `05-testing` on, the same applies to the feature folders
students add to the test projects.

Also keep stable the API that student code compiles against:

- `IDeliveryStrategy<TCommand>`, `IDeliveryCommand`, `DeliveryQuote`;
- the constructor of `DeliveryQuoteHandler<TCommand>` (`strategy`, `timeProvider`).
  Cross-cutting concerns go to MediatR behaviors, not to this constructor: a new parameter breaks every student handler.

## Adding or changing a step

Each step consists of:

| What                     | Where                                                          |
|--------------------------|----------------------------------------------------------------|
| Task description         | `tasks/NN-topic.md`                                            |
| Automatic checks         | `tests/TrainingApp.Checks/TaskNN*Checks.cs`, `[Trait("Task", "NN")]` |
| Infrastructure           | `src/TrainingApp.Api/...`, `monitoring/...`                    |
| Reference examples       | the built-in features in `src/TrainingApp.Api/Features/`       |

Checks find the student's feature by reflection (`StudentFeature`), never by name,
and their failure messages must say what to do, not only what is wrong.

To add a step, create the branch from the last one, and add its name to the branch list in
`scripts/sync-branches.ps1`, `scripts/sync-branches.sh` and to the course map in `README.md` (on `main`).

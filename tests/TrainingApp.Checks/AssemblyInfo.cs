// Checks listen to process-wide telemetry (meters, activity sources): run them one at a time
// so a check never sees measurements or spans produced by another one.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

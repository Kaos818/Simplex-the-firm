using Xunit;

// Each fixture owns an isolated relational database. Serial execution avoids native SQLite
// connection contention while concurrency is tested explicitly inside the financial tests.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

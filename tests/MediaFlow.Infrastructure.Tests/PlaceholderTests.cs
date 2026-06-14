// Integration tests go here once infrastructure adapters are implemented.
// These tests run against real files, real FFmpeg, and real LiteDB — no mocks.
// Mark slow tests with [Trait("Category", "Integration")] to allow selective exclusion in CI.

namespace MediaFlow.Infrastructure.Tests;

public class PlaceholderTests
{
    [Fact]
    public void Placeholder_PassesUntilAdaptersAreAdded() => Assert.True(true);
}

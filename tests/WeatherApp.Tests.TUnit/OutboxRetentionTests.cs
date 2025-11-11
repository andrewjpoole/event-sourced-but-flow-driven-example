using Shouldly;
using WeatherApp.Tests.TUnit.Framework.Persistence;
using WeatherApp.Infrastructure.Outbox;

namespace WeatherApp.Tests.TUnit;

public class OutboxRetentionTests
{
    [Before(Test)]
    public void Setup() { }

    [Test]
    public async Task Removes_only_sent_items_older_than_cutoff()
    {
        var repo = new OutboxRepositoryInMemory();

        // item 1: sent long ago
        var item1 = new OutboxItem(
            -1,
            Guid.NewGuid().ToString(),
            "A",
            "{}",
            "A",
            DateTimeOffset.UtcNow.AddDays(-60));
    var id1 = await repo.Add(item1);
    var sentStatus1 = new OutboxSentStatusUpdate(OutboxConstants.NoIdYet, id1, OutboxSentStatus.Sent, null, DateTimeOffset.UtcNow.AddDays(-61));
    await repo.AddSentStatus(sentStatus1);
    // ensure the status was recorded
    repo.OutboxItems[id1].StatusUpdates.Count.ShouldBeGreaterThan(0);

        // item 2: sent recently
        var item2 = new OutboxItem(
            -1,
            Guid.NewGuid().ToString(),
            "B",
            "{}",
            "B",
            DateTimeOffset.UtcNow.AddDays(-10));
    var id2 = await repo.Add(item2);
    var sentStatus2 = new OutboxSentStatusUpdate(OutboxConstants.NoIdYet, id2, OutboxSentStatus.Sent, null, DateTimeOffset.UtcNow.AddDays(-10));
    await repo.AddSentStatus(sentStatus2);
    repo.OutboxItems[id2].StatusUpdates.Count.ShouldBeGreaterThan(0);

        // item 3: not sent
        var item3 = new OutboxItem(
            -1,
            Guid.NewGuid().ToString(),
            "C",
            "{}",
            "C",
            DateTimeOffset.UtcNow.AddDays(-2));
        var id3 = await repo.Add(item3);

        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
    // perform retention

    var removed = await repo.RemoveSentOutboxItemsOlderThan(cutoff);

    removed.ShouldBe(1);
        repo.OutboxItems.ContainsKey(id1).ShouldBeFalse();
        repo.OutboxItems.ContainsKey(id2).ShouldBeTrue();
        repo.OutboxItems.ContainsKey(id3).ShouldBeTrue();
    }
}

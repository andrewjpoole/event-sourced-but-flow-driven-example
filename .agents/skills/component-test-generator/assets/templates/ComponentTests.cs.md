# ComponentTests template

Purpose: provide a readable TUnit starter test suite that exercises the generated fixture and Given/When/Then helpers. Adapt the scenario names, request types, domain events, and message types for the target app.

```csharp
using System.Net;
using Shouldly;
using {Namespace}.Application.Models;
using {Namespace}.Application.IntegrationEvents;
using {Namespace}.Domain.DomainEvents;

namespace {Namespace}.Tests.TUnit;

public sealed class ComponentTests
{
    private ComponentTestFixture testFixture = null!;

    [Before(Test)]
    public void Setup()
    {
        testFixture = new ComponentTestFixture();
    }

    [After(Test)]
    public void TearDown()
    {
        testFixture.Dispose();
    }

    [Test]
    public void Returns_success_given_valid_request()
    {
        var (given, when, then, cannedData) = testFixture.SetupHelpers();

        given
            .TheServersAreStarted();

        when
            .WeWrap{RequestName}InAnHttpRequestMessage(new {RequestType}(), cannedData, cannedData.Reference, out var request)
            .And.WeSendTheMessageToTheApi(request, out var response);

        then
            .TheResponseCodeShouldBe(response, HttpStatusCode.OK)
            .And.TheBodyShouldNotBeEmpty<{ResponseType}>(response, body =>
            {
                body.ShouldNotBeNull();
            });
    }

    [Test]
    public void EndToEnd_flow_completes_successfully()
    {
        var (given, when, then, cannedData) = testFixture.SetupHelpers();

        given
            .The{ExternalService1}EndpointWillReturn(HttpStatusCode.OK)
            .And.TheServersAreStarted();

        when
            .InPhase("1 (send initial API request)")
            .And.WeWrap{RequestName}InAnHttpRequestMessage(new {RequestType}(), cannedData, cannedData.Reference, out var request)
            .And.WeSendTheMessageToTheApi(request, out var response);

        then
            .InPhase("1 (verify initial API response)")
            .And.TheResponseCodeShouldBe(response, HttpStatusCode.OK)
            .And.TheDomainEventShouldHaveBeenPersisted<{InitiatedEventType}>()
            .And.WeGetTheStreamIdFromTheInitialDomainEvent(cannedData.RequestId, out var streamId);

        when
            .InPhase("2 (inject inbound integration event)")
            .AMessageAppears(new {InboundEvent1}(streamId));

        then
            .InPhase("2 (verify worker handling)")
            .And.TheMessageWasHandled<{InboundEvent1}>()
            .And.TheDomainEventShouldHaveBeenPersisted<{DomainEvent2}>();

        then
            .InPhase("3 (advance fake time for outbox)")
            .AfterSomeTimeHasPassed()
            .And.AnOutboxRecordWasInserted<{OutboundEvent1}>()
            .And.AMessageWasSent<{OutboundEvent1}>();
    }
}
```

Notes:

- Use `[Before(Test)]`, `[After(Test)]`, and `[Test]` for TUnit.
- Add more tests beside `ComponentTests.cs` when scenarios become numerous; the DSL can be shared across files.
- Multi-phase tests are the default shape for flows that cross API, event listener, and outbox worker boundaries.

# event-sourced-but-flow-driven-example

An example repo which uses fluent method chaining and domain events to achieve an eventsourced but flow-driven architecture

I use it to try things out, to demonstrate things in talks and to share with anyone who might find it useful.

## Interesting things in this repo:

- Fluent method chaining to create super clear orchestration code
- Use of OneOf discriminated unions library to avoid use of exceptions for non-exception scenarios
- E2e component tests (lots of cool stuff in here😁)
    - Using FakeTimeProvider to control the time during tests
- Retryable Dapper db connection with transactions
- Event sourcing using a SQL table of domain events
- Aspire providing a sublime local dev experience
- OTEL tracing
- Integration tests
- A Source Generator which will take constants and place them in an immutable dictionary in a partial class, for classes decorated with an attribute, very handy for service bus Type -> entity name mapping 😊

## Scenario outline

coming soon
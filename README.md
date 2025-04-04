# event-sourced-but-flow-driven-example

An example repo which uses fluent method chaining and domain events to achieve an eventsourced but flow-driven architecture

I use it to try things out, to demonstrate things in talks and to share with anyone who might find it useful.

## Interesting things in this repo:

- fluent method chaining to create super clear orchestration code
- use of OneOf discriminated unions library to avoid use of exceptions for non-exception scenarios
- e2e component tests (lots of cool stuff in here😁)
- retryable Dapper db connection with transactions
- event sourcing using a SQL table
- (coming soon) integration tests which setup your local dev environment for you!

## Scenario outline

coming soon
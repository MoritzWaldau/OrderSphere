# Notification Service

## Structure

This service contains **one project only**: `OrderSphere.Notification.Worker`.

There is no Api, Application, Domain, or Infrastructure project. This is the single documented exception to the 4-project rule stated in the root `CLAUDE.md`. The exception is intentional: notifications are fire-and-forget consumers with no domain of their own.

**Do not add layers.** Introducing an Api, Application, Domain, or Infrastructure project requires an explicit architecture review.

## Event consumption

The worker consumes integration events from Azure Service Bus via the Inbox (`BuildingBlocks.EventBus`). New notification triggers are implemented by adding an `IIntegrationEventHandler<T>` inside the Worker project.

## No outbox

The worker does not publish integration events. There is no `DbContext` and no EF infrastructure in this service.

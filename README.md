# ESFA-DC-Queueing

## Introduction

Provides publishing and subscription functionality for Azure Service Bus queues.

## Usage

Separate packages exist for the interface and implementation.

IQueueConfiguration (QueueConfiguration) should be populated with the relevant queue configuration.

For sending messages to a queue, the IQueuePublishService\<T> (QueuePublishService\<T>) provides a PublishAsync method. The T object will be serialised and enqueued.

The IQueueSubscriptionService\<T> (QueueSubscriptionService\<T>) provides Subscribe and UnsubscribeAsync methods for registering and deregistering a callback to fire when a new message is received. The T object will be deserialised from the queue message.

## Package Dependencies

- Logger
- Serialization
- <i>Microsoft.Azure.ServiceBus</i>
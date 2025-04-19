using System.Text.Json;

namespace WeatherApp.Domain.EventSourcing;

public class Event
{
    protected Event(Guid streamId, int version, string eventClassName, string serialisedEvent, Dictionary<string, object>? additionalFields = null)
    {
        StreamId = streamId;
        Version = version;
        EventClassName = eventClassName;
        SerialisedEvent = serialisedEvent;
        AdditionalFields = AdditionalFields;
    }

    public Guid StreamId { get; protected set; }
    public int Version { get; protected set; }
    public Guid LocationId { get; protected set; }
    public object? Value { get; protected set; }
    public string EventClassName { get; protected set; }
    public string SerialisedEvent { get; protected set; }
    public Dictionary<string, object>? AdditionalFields { get; protected set; }

    public static Event Create<T>(T value, Guid streamId, int version, Dictionary<string, object>? additionalFields = null) where T : IDomainEvent
    {
        var @event = new Event(streamId, version, typeof(T).FullName!, JsonSerializer.Serialize(value, GlobalJsonSerialiserSettings.Default), additionalFields)
        {
            Value = value
        };
        return @event;
    }
}
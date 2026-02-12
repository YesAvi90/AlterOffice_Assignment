using System;
using System.Collections.Generic;

public static class GameEventBus
{
    private static Dictionary<GameEvent, Action<object>> eventTable =
        new Dictionary<GameEvent, Action<object>>();

    public static void Subscribe(GameEvent gameEvent, Action<object> listener)
    {
        if (!eventTable.ContainsKey(gameEvent))
            eventTable[gameEvent] = delegate { };

        eventTable[gameEvent] += listener;
    }

    public static void Unsubscribe(GameEvent gameEvent, Action<object> listener)
    {
        if (!eventTable.ContainsKey(gameEvent))
            return;

        eventTable[gameEvent] -= listener;
    }

    public static void Raise(GameEvent gameEvent, object payload = null)
    {
        if (eventTable.ContainsKey(gameEvent))
            eventTable[gameEvent]?.Invoke(payload);
    }
}

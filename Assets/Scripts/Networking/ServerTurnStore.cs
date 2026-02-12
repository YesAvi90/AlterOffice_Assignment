using System.Collections.Generic;
using UnityEngine;

public static class ServerTurnStore
{
    private static Dictionary<string, EndTurnPayload> turns = new Dictionary<string, EndTurnPayload>();

    public static void Store(EndTurnPayload payload)
    {
        turns[payload.playerId] = payload;

        Debug.Log($"[SERVER] Stored turn for {payload.playerId}");
    }

    public static EndTurnPayload Get(string playerId)
    {
        return turns.TryGetValue(playerId, out var payload) ? payload: null;
    }

    public static bool BothPlayersReady()
    {
        return turns.Count == 2;
    }

    public static void Clear()
    {
        turns.Clear();
    }
}

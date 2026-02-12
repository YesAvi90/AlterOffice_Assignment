using System.Collections.Generic;

[System.Serializable]
public class EndTurnPayload
{
    public string playerId;
    public List<PlayerCardData> cards;
}

using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameMessage
{
    public string type;
    public string payload;
}

[System.Serializable]
public class ScorePayload
{
    public int p1;
    public int p2;
}

[System.Serializable]
public class RevealPayload
{
    public string playerId;
    public PlayerCardData card;
}

[System.Serializable]
public class OpponentPlayedPayload
{
    public string playerId;
    public int count;
}

[System.Serializable]
public class RemovePlayedPayload
{
    public string ownerPlayerId;
    public int cardId;
}

public class NetworkRelay : NetworkBehaviour
{
    [SyncVar] public string PlayerId;

    public override void OnStartServer()
    {
        if (NetworkServer.connections.Count == 1)
            PlayerId = "P1";
        else
        {
            PlayerId = "P2";

            NetworkTurnManager tm = FindObjectOfType<NetworkTurnManager>();
            if (tm != null)
                tm.Server_SetGameReady();
        }
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer) return;
        PlayerIdentity.LocalPlayerId = PlayerId;
    }

    [Server]
    private void SendGameMessage(string type, object payloadObj)
    {
        GameMessage msg = new GameMessage
        {
            type = type,
            payload = payloadObj != null
                ? JsonUtility.ToJson(payloadObj)
                : ""
        };

        RpcGameMessage(JsonUtility.ToJson(msg));
    }

    [ClientRpc]
    private void RpcGameMessage(string json)
    {
        GameMessage msg = JsonUtility.FromJson<GameMessage>(json);

        switch (msg.type)
        {
            case "EnterReveal":
                CanvasControl.Instance?.ForceSetEndTurnButton(false);
                RoundUI.Instance?.ShowRevealing();
                IPAddressUI.Instance?.Clear();
                break;

            case "RevealCard":
                RevealPayload reveal =
                    JsonUtility.FromJson<RevealPayload>(msg.payload);

                CardData data =
                    CardDatabaseLoader.Instance.GetCardById(reveal.card.cardId);

                if (reveal.playerId == PlayerIdentity.LocalPlayerId)
                    PlayedCardsUI.Instance.RevealCardAt(reveal.card.order, data);
                else
                    OpponentPlayedCardsUI.Instance.RevealCardAt(reveal.card.order, data);
                break;

            case "ScoreUpdated":
                ScorePayload score =
                    JsonUtility.FromJson<ScorePayload>(msg.payload);

                ScoreUI.Instance.UpdateScores(score.p1, score.p2);
                break;

            case "OpponentPlayed":
                OpponentPlayedPayload opp =
                    JsonUtility.FromJson<OpponentPlayedPayload>(msg.payload);

                if (opp.playerId == PlayerIdentity.LocalPlayerId)
                    break;

                List<PlayedCard> blanks = new();
                for (int i = 0; i < opp.count; i++)
                {
                    blanks.Add(new PlayedCard
                    {
                        cardId = -1,
                        order = i
                    });
                }

                OpponentPlayedCardsUI.Instance?.AddFoldedCards(blanks);
                break;

            case "RemovePlayed":
                RemovePlayedPayload remove =
                    JsonUtility.FromJson<RemovePlayedPayload>(msg.payload);

                if (remove.ownerPlayerId == PlayerIdentity.LocalPlayerId)
                    PlayedCardsUI.Instance?.RemoveCardById(remove.cardId);
                else
                    OpponentPlayedCardsUI.Instance?.RemoveCardById(remove.cardId);
                break;
        }
    }



    [Command]
    public void CmdSubmitEndTurn(string payloadJson)
    {
        EndTurnPayload payload =
            JsonUtility.FromJson<EndTurnPayload>(payloadJson);

        ServerTurnStore.Store(payload);

        SendGameMessage("OpponentPlayed",
            new OpponentPlayedPayload
            {
                playerId = payload.playerId,
                count = payload.cards.Count
            });

        NetworkTurnManager tm = FindObjectOfType<NetworkTurnManager>();

        if (!ServerTurnStore.BothPlayersReady())
        {
            if (tm != null)
                tm.ServerAdvanceTurn();
            return;
        }

        EndTurnPayload p1 = ServerTurnStore.Get("P1");
        EndTurnPayload p2 = ServerTurnStore.Get("P2");

        if (tm != null)
            StartCoroutine(ServerRevealSequence(p1, p2, tm));
    }



    [Server]
    public IEnumerator ServerRevealSequence(EndTurnPayload p1, EndTurnPayload p2, NetworkTurnManager tm)
    {
        tm.Server_StopTurnTimer();

        SendGameMessage("EnterReveal", null);

        string initiativePlayer = tm.RoundInitiativePlayerId;
        string otherPlayer = initiativePlayer == "P1" ? "P2" : "P1";

        var first = initiativePlayer == "P1" ? p1.cards : p2.cards;
        var second = initiativePlayer == "P1" ? p2.cards : p1.cards;

        int max = Mathf.Max(first.Count, second.Count);

        yield return new WaitForSeconds(1.5f);

        for (int i = 0; i < max; i++)
        {
            if (i < first.Count)
                yield return StartCoroutine(
                    RevealAndResolveSingle(
                        initiativePlayer,
                        first[i],
                        first.Count,
                        tm));

            if (i < second.Count)
                yield return StartCoroutine(
                    RevealAndResolveSingle(
                        otherPlayer,
                        second[i],
                        second.Count,
                        tm));
        }

        yield return new WaitForSeconds(1f);

        tm.Server_AdvanceRound();
        ServerTurnStore.Clear();
    }

    [Server]
    private IEnumerator RevealAndResolveSingle(string playerId, PlayerCardData cardData, int cardsPlayedThisTurn, NetworkTurnManager tm)
    {
        SendGameMessage("RevealCard",
            new RevealPayload
            {
                playerId = playerId,
                card = cardData
            });

        yield return new WaitForSeconds(1f);

        CardData data =
            CardDatabaseLoader.Instance.GetCardById(cardData.cardId);

        tm.AddScore(playerId, data.power);
        tm.RegisterPlayedCard(playerId, data);

        ApplyAbility(playerId, data, cardsPlayedThisTurn, tm);

        SendGameMessage("ScoreUpdated",
            new ScorePayload
            {
                p1 = tm.GetP1Score(),
                p2 = tm.GetP2Score()
            });

        yield return new WaitForSeconds(1f);
    }


    [Server]
    private void ApplyAbility(string playerId, CardData data, int cardsPlayedThisTurn, NetworkTurnManager tm)
    {
        if (data.ability == null)
            return;

        switch (data.ability.type)
        {
            case "StealPoints":
                string opponent = playerId == "P1" ? "P2" : "P1";

                int opponentScore =
                    opponent == "P1" ? tm.GetP1Score() : tm.GetP2Score();

                int steal = Mathf.Min(data.ability.value, opponentScore);

                tm.AddScore(playerId, steal);
                tm.AddScore(opponent, -steal);
                break;

            case "BonusPoints":
                tm.AddScore(playerId, cardsPlayedThisTurn);
                break;

            case "DrawCard":
                tm.Server_DrawExtraCard(playerId);
                break;

            case "DiscardOpponentCard":
                DiscardOpponentPlayedCard(playerId, tm);
                break;
        }
    }

    [Server]
    private void DiscardOpponentPlayedCard(string playerId, NetworkTurnManager tm)
    {
        string opponent = playerId == "P1" ? "P2" : "P1";

        CardData removed = tm.RemoveRandomPlayedCard(opponent);

        if (removed == null)
            return;

        tm.AddScore(opponent, -removed.power);

        SendGameMessage("RemovePlayed",
            new RemovePlayedPayload
            {
                ownerPlayerId = opponent,
                cardId = removed.id
            });
    }

    [Command]
    public void CmdRequestExitMatch()
    {
        Matchmaking mm = FindObjectOfType<Matchmaking>();
        if (mm != null)
            mm.StopHost();
    }
}

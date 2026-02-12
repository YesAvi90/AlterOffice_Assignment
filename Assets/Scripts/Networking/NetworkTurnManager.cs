using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkTurnManager : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnActivePlayerChanged))]
    private string activePlayerId;

    [SyncVar(hook = nameof(OnGameReadyChanged))]
    private bool gameReady;

    [SyncVar(hook = nameof(OnRoundChanged))]
    private int currentRound;

    [SyncVar(hook = nameof(OnTimeRemainingChanged))]
    private int timeRemaining;

    public string ActivePlayerId => activePlayerId;
    public bool GameReady => gameReady;

    private Dictionary<string, int> energyByPlayer = new();
    private Dictionary<string, Queue<int>> deckByPlayer = new();

    private const int MAX_ROUNDS = 6;
    private const int TURN_DURATION = 30;

    private Coroutine turnTimerRoutine;

    private int p1Score = 0;
    private int p2Score = 0;

    public int GetP1Score() => p1Score;
    public int GetP2Score() => p2Score;

    private string roundInitiativePlayerId;
    public string RoundInitiativePlayerId => roundInitiativePlayerId;

    private Dictionary<string, List<CardData>> playedHistoryByPlayer = new();



    [Server]
    public void AddScore(string playerId, int amount)
    {
        if (playerId == "P1")
            p1Score += amount;
        else
            p2Score += amount;
    }

    [Server]
    public void ResetScores()
    {
        p1Score = 0;
        p2Score = 0;
    }

    public override void OnStartServer()
    {
        Debug.Log("[SERVER] Fresh match state initialized");

        activePlayerId = "P1";
        gameReady = false;
        currentRound = 0;

        p1Score = 0;
        p2Score = 0;

        energyByPlayer.Clear();
        deckByPlayer.Clear();
        playedHistoryByPlayer.Clear();
    }


    [Server]
    public void RegisterPlayedCard(string playerId, CardData data)
    {
        if (!playedHistoryByPlayer.ContainsKey(playerId))
            return;

        playedHistoryByPlayer[playerId].Add(data);
    }


    [Server]
    public void Server_SetGameReady()
    {
        gameReady = true;
        currentRound = 1;

        ResetScores();

        playedHistoryByPlayer.Clear();
        playedHistoryByPlayer["P1"] = new List<CardData>();
        playedHistoryByPlayer["P2"] = new List<CardData>();


        // Initiative decides first player
        roundInitiativePlayerId = CalculateInitiative();
        activePlayerId = roundInitiativePlayerId;


        // Energy setup
        energyByPlayer.Clear();
        energyByPlayer["P1"] = 1;
        energyByPlayer["P2"] = 1;

        // Build decks
        deckByPlayer.Clear();
        deckByPlayer["P1"] = BuildShuffledDeck();
        deckByPlayer["P2"] = BuildShuffledDeck();

        // Draw 3 starting cards
        List<int> p1Hand = DrawCards("P1", 3);
        List<int> p2Hand = DrawCards("P2", 3);

        RpcInitStartingHand(p1Hand, p2Hand);
        RpcInitRoundState(1, 1);

        StartCoroutine(StartTurnTimerFirstTime());
    }

    [Server]
    private IEnumerator StartTurnTimerFirstTime()
    {
        yield return new WaitForSeconds(3f);
        StartTurnTimer();
    }

    [Server]
    public void ServerAdvanceTurn()
    {
        activePlayerId = activePlayerId == "P1" ? "P2" : "P1";
        StartTurnTimer();
    }

    [Server]
    public void Server_AdvanceRound()
    {
        if (currentRound >= MAX_ROUNDS)
        {
            Server_EndMatch();
            return;
        }

        currentRound++;

        roundInitiativePlayerId = CalculateInitiative();
        activePlayerId = roundInitiativePlayerId;


        int energyThisRound = currentRound;

        energyByPlayer["P1"] = energyThisRound;
        energyByPlayer["P2"] = energyThisRound;

        List<int> p1Draw = DrawCards("P1", 1);
        List<int> p2Draw = DrawCards("P2", 1);

        RpcAddCardToHand(p1Draw, p2Draw);
        RpcInitRoundEnergy(energyThisRound, energyThisRound);

        StartTurnTimer();

        RpcForceTurnRefresh(activePlayerId, gameReady);
    }

    [ClientRpc]
    private void RpcForceTurnRefresh(string activePlayer, bool ready)
    {
        if (CanvasControl.Instance != null)
            CanvasControl.Instance.RefreshTurnUI(activePlayer, ready);
    }


    [Server]
    private void Server_EndMatch()
    {
        int p1Score = GetP1Score();
        int p2Score = GetP2Score();


        string winner = "Draw";

        if (p1Score > p2Score) winner = "P1";
        else if (p2Score > p1Score) winner = "P2";

        RpcMatchEnded(winner);
    }

    [Server]
    private void StartTurnTimer()
    {
        if (turnTimerRoutine != null)
            StopCoroutine(turnTimerRoutine);

        timeRemaining = TURN_DURATION;
        turnTimerRoutine = StartCoroutine(TurnTimer());
    }

    private IEnumerator TurnTimer()
    {
        while (timeRemaining > 0)
        {
            yield return new WaitForSeconds(1f);
            timeRemaining--;
        }

        HandleTurnTimeout();
    }

    [Server]
    private void HandleTurnTimeout()
    {
        string playerId = activePlayerId;

        Debug.Log($"[SERVER] {playerId} timed out");

        // Build empty turn
        EndTurnPayload payload = new EndTurnPayload
        {
            playerId = playerId,
            cards = new List<PlayerCardData>()
        };

        ServerTurnStore.Store(payload);

        if (!ServerTurnStore.BothPlayersReady())
        {
            ServerAdvanceTurn();
            return;
        }

        // If both ready → reveal
        NetworkRelay relay = FindObjectOfType<NetworkRelay>();
        if (relay != null)
        {
            EndTurnPayload p1 = ServerTurnStore.Get("P1");
            EndTurnPayload p2 = ServerTurnStore.Get("P2");

            StartCoroutine(relay.ServerRevealSequence(p1, p2, this));
        }
    }


    [Server]
    public void Server_StopTurnTimer()
    {
        if (turnTimerRoutine != null)
            StopCoroutine(turnTimerRoutine);

        turnTimerRoutine = null;
        timeRemaining = 0;
    }

    [Server]
    public string CalculateInitiative()
    {
        if (p1Score > p2Score) return "P1";
        if (p2Score > p1Score) return "P2";



        return Random.value < 0.5f ? "P1" : "P2";
    }


    private void OnActivePlayerChanged(string _, string newPlayer)
    {
        if (!gameReady) return;
        if (!CanvasControl.Instance.IsGameStarted()) return;

        bool isMyTurn =
            newPlayer == PlayerIdentity.LocalPlayerId;

        RoundUI.Instance?.UpdateRoundWithTurn(currentRound, isMyTurn);
        CanvasControl.Instance?.RefreshTurnUI(newPlayer, gameReady);
    }

    private void OnGameReadyChanged(bool _, bool ready)
    {
        CanvasControl.Instance?.RefreshTurnUI(activePlayerId, ready);
    }

    private void OnRoundChanged(int _, int newRound)
    {
        if (!gameReady) return;
        if (!CanvasControl.Instance.IsGameStarted()) return;

        bool isMyTurn =
            activePlayerId == PlayerIdentity.LocalPlayerId;

        RoundUI.Instance?.UpdateRoundWithTurn(newRound, isMyTurn);
    }

    private void OnTimeRemainingChanged(int _, int newValue)
    {
        if (!CanvasControl.Instance.IsGameStarted()) return;

        if (newValue > 0)
            IPAddressUI.Instance?.ShowRemainingTime(newValue);
        else
            IPAddressUI.Instance?.Clear();
    }

    [ClientRpc]
    private void RpcInitRoundState(int p1Energy, int p2Energy)
    {
        int myEnergy =
            PlayerIdentity.LocalPlayerId == "P1"
            ? p1Energy : p2Energy;

        EnergyUI.Instance.UpdateEnergy(myEnergy);
        ScoreUI.Instance.UpdateScores(0, 0);
        PlayerHand.Instance.SetAvailableCost(myEnergy);

        RoundUI.Instance?.PlayGameStartSequence(currentRound);
        IPAddressUI.Instance?.Clear();

        PlayerHand.Instance.selected.Clear();

    }

    [ClientRpc]
    private void RpcInitRoundEnergy(int p1Energy, int p2Energy)
    {
        int myEnergy =
            PlayerIdentity.LocalPlayerId == "P1"
            ? p1Energy : p2Energy;

        EnergyUI.Instance.UpdateEnergy(myEnergy);
        PlayerHand.Instance.SetAvailableCost(myEnergy);

        PlayerHand.Instance.selected.Clear();
    }


    [ClientRpc]
    private void RpcInitStartingHand(
        List<int> p1Cards,
        List<int> p2Cards)
    {
        List<int> myIds =
            PlayerIdentity.LocalPlayerId == "P1"
            ? p1Cards : p2Cards;

        List<CardData> cards = new();

        foreach (int id in myIds)
            cards.Add(CardDatabaseLoader.Instance.GetCardById(id));

        StartingHandCache.Cache(cards);
    }

    [ClientRpc]
    private void RpcAddCardToHand(
        List<int> p1Cards,
        List<int> p2Cards)
    {
        List<int> myIds =
            PlayerIdentity.LocalPlayerId == "P1"
            ? p1Cards : p2Cards;

        foreach (int id in myIds)
        {
            CardData data =
                CardDatabaseLoader.Instance.GetCardById(id);

            PlayerHand.Instance.hand.Add(
                new CardInstance(data));
        }

        PlayerHandUI.Instance.BuildHand();
    }

    [ClientRpc]
    private void RpcMatchEnded(string winner)
    {
        bool didIWin =
            winner == PlayerIdentity.LocalPlayerId;

        if (winner == "Draw")
            CanvasControl.Instance.ShowMatchResult("Draw");
        else
            CanvasControl.Instance.ShowMatchResult(
                didIWin ? "You Win" : "You Lose");
    }


    [Server]
    private Queue<int> BuildShuffledDeck()
    {
        List<int> deck = new();
        var allCards = CardDatabaseLoader.Instance.Cards;

        for (int i = 0; i < Mathf.Min(12, allCards.Length); i++)
            deck.Add(allCards[i].id);

        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        return new Queue<int>(deck);
    }

    [Server]
    public List<int> DrawCards(string playerId, int count)
    {
        List<int> drawn = new();
        var deck = deckByPlayer[playerId];

        for (int i = 0; i < count && deck.Count > 0; i++)
            drawn.Add(deck.Dequeue());

        return drawn;
    }

    public int GetCurrentRound() => currentRound;

    [Server]
    public void Server_DrawExtraCard(string playerId)
    {
        List<int> draw = DrawCards(playerId, 1);

        if (playerId == "P1")
            RpcAddCardToHand(draw, new List<int>());
        else
            RpcAddCardToHand(new List<int>(), draw);
    }


    [Server]
    public CardData RemoveRandomPlayedCard(string playerId)
    {
        if (!playedHistoryByPlayer.ContainsKey(playerId))
            return null;

        var list = playedHistoryByPlayer[playerId];

        if (list.Count == 0)
            return null;

        int index = Random.Range(0, list.Count);
        CardData removed = list[index];

        list.RemoveAt(index);

        return removed;
    }


}

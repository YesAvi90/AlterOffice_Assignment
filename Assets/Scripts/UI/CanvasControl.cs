using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasControl : MonoBehaviour
{
    public static CanvasControl Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI debugMsg;


    [Header("Gameplay UI")]
    [SerializeField] private Button endTurnButton;

    [Header("Matchmaking UI")]
    [SerializeField] private GameObject matchmakingPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;

    [Header("Result UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultHeaderText;
    [SerializeField] private TextMeshProUGUI resultBodyText;
    [SerializeField] private Button rematchButton;
    [SerializeField] private Button quitButton;

    private bool gameHasStarted = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        endTurnButton.onClick.AddListener(OnEndTurnClicked);

        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        rematchButton.onClick.AddListener(OnRematchClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        ShowMatchmaking();

        SetEndTurnButton(false);
    }

    public void ShowDebugMessage(string message)
    {
        if (debugMsg != null)
            debugMsg.text = message;
    }

    public void ShowMatchmakingUIWithMessage(string message)
    {
        resultPanel.SetActive(false);
        ShowMatchmaking();
        ShowDebugMessage(message);
    }


    public void RefreshTurnUI(string activePlayerId, bool ready)
    {
        if (!ready || !gameHasStarted)
        {
            SetEndTurnButton(false);
            return;
        }

        bool isMyTurn = activePlayerId == PlayerIdentity.LocalPlayerId;

        SetEndTurnButton(isMyTurn);
    }

    public void NotifyGameStarted()
    {
        gameHasStarted = true;

        var tm = FindObjectOfType<NetworkTurnManager>();
        if (tm != null)
            RefreshTurnUI(tm.ActivePlayerId, tm.GameReady);
    }

    private void OnEndTurnClicked()
    {
        PlayerHand hand = PlayerHand.Instance;

        var selectedCards = hand.GetSelectedCards();

        List<PlayedCard> folded = new List<PlayedCard>();

        int order = 0;
        int spentEnergy = 0;

        foreach (var card in selectedCards)
        {
            folded.Add(new PlayedCard
            {
                cardId = card.Id,
                order = order++
            });

            spentEnergy += card.Cost;
        }

        // Deduct energy
        EnergyUI.Instance.UpdateEnergy(0);


        // Remove from hand
        foreach (var card in selectedCards)
            hand.hand.Remove(card);

        hand.selected.Clear();
        hand.playedThisTurn = folded;

        PlayerHandUI.Instance.BuildHand();

        // Show folded face-down cards locally
        PlayedCardsUI.Instance.AddFoldedCards(folded);


        // Build JSON payload
        EndTurnPayload payload = hand.BuildEndTurnPayload();
        string json = JsonUtility.ToJson(payload);

        NetworkRelay relay =
            NetworkClient.localPlayer.GetComponent<NetworkRelay>();

        relay.CmdSubmitEndTurn(json);
    }

    private void OnHostClicked()
    {
        // Reset EVERYTHING before starting a new session
        ResetGameState();

        var mm = FindObjectOfType<Matchmaking>();
        mm.StopHost();   // ensure fully stopped if somehow still running
        mm.StartHost();

        ShowGameplay();

        RoundUI.Instance.ShowWaitingForOpponent();
        IPAddressUI.Instance.ShowLocalIP();
    }

    private void OnJoinClicked()
    {
        var mm = FindObjectOfType<Matchmaking>();
        mm.networkAddress = ipInputField.text.Trim();
        mm.StartClient();
        ShowGameplay();
    }

    private void ShowMatchmaking()
    {
        matchmakingPanel.SetActive(true);
        gameplayPanel.SetActive(false);
    }

    private void ShowGameplay()
    {
        matchmakingPanel.SetActive(false);
        gameplayPanel.SetActive(true);
    }

    private void SetEndTurnButton(bool enabled)
    {
        endTurnButton.interactable = enabled;
    }

    public void ShowMatchResult(string resultText)
    {
        gameplayPanel.SetActive(false);
        resultPanel.SetActive(true);

        resultHeaderText.text = "Match Over";
        resultBodyText.text = resultText;

        SetEndTurnButton(false);

        rematchButton.interactable = false;
        quitButton.interactable = false;

        StartCoroutine(EnableResultButtonsAfterDelay());
    }

    private IEnumerator EnableResultButtonsAfterDelay()
    {
        yield return new WaitForSeconds(4f);

        rematchButton.interactable = true;
        quitButton.interactable = true;
    }



    private void OnRematchClicked()
    {
        FindObjectOfType<Matchmaking>().StopHost();
        resultPanel.SetActive(false);
        ShowMatchmaking();
    }

    private void OnQuitClicked()
    {
        if (NetworkClient.localPlayer == null)
            return;

        NetworkRelay relay =
            NetworkClient.localPlayer.GetComponent<NetworkRelay>();

        if (relay != null)
            relay.CmdRequestExitMatch();
    }


    public bool IsGameStarted() => gameHasStarted;


    public void ResetGameState()
    {
        Debug.Log("[UI] Resetting game state");

        gameHasStarted = false;

        // Clear player hand
        if (PlayerHand.Instance != null)
        {
            PlayerHand.Instance.hand.Clear();
            PlayerHand.Instance.selected.Clear();
            PlayerHand.Instance.ClearPlayedThisTurn();
            PlayerHand.Instance.SetAvailableCost(0);
        }

        if (PlayerHandUI.Instance != null)
            PlayerHandUI.Instance.BuildHand();

        // Clear played cards
        if (PlayedCardsUI.Instance != null)
            PlayedCardsUI.Instance.Clear();

        if (OpponentPlayedCardsUI.Instance != null)
            OpponentPlayedCardsUI.Instance.ClearAll();

        // Reset UI values
        if (EnergyUI.Instance != null)
            EnergyUI.Instance.UpdateEnergy(0);

        if (ScoreUI.Instance != null)
            ScoreUI.Instance.UpdateScores(0, 0);

        if (RoundUI.Instance != null)
            RoundUI.Instance.ShowWaitingForOpponent();

        if (IPAddressUI.Instance != null)
            IPAddressUI.Instance.Clear();

        StartingHandCache.Reset();
    }

    public void ForceSetEndTurnButton(bool value)
    {
        SetEndTurnButton(value);
    }



}

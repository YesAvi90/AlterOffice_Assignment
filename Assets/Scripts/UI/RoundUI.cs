using TMPro;
using UnityEngine;
using System.Collections;

public class RoundUI : MonoBehaviour
{
    public static RoundUI Instance;

    [SerializeField] private TextMeshProUGUI roundText;

    private Coroutine roundSequenceRoutine;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowWaitingForOpponent()
    {
        StopSequenceIfRunning();
        roundText.text = "Waiting for Opponent";
    }


    public void PlayGameStartSequence(int roundNumber)
    {
        StopSequenceIfRunning();
        roundSequenceRoutine = StartCoroutine(GameStartSequence(roundNumber));
    }

    private IEnumerator GameStartSequence(int roundNumber)
    {
        roundText.text = "Opponent Found";
        yield return new WaitForSeconds(1f);

        roundText.text = "Starting Game";
        yield return new WaitForSeconds(1f);

        CanvasControl.Instance.NotifyGameStarted();
        StartingHandCache.NotifyGameStartSequenceFinished();


        var tm = FindObjectOfType<NetworkTurnManager>();
        if (tm != null)
        {
            bool isMyTurn = tm.ActivePlayerId == PlayerIdentity.LocalPlayerId;
            UpdateRoundWithTurn(tm.GetCurrentRound(), isMyTurn);
        }
    }





    public void UpdateRound(int round)
    {
        StopSequenceIfRunning();
        roundText.text = $"Round {round}";
    }

    public void UpdateRoundWithTurn(int round, bool isMyTurn)
    {
        string turnText = isMyTurn ? "Your Turn" : "Opponent's Turn";
        roundText.text = $"Round {round}: {turnText}";
    }

    private void StopSequenceIfRunning()
    {
        if (roundSequenceRoutine != null)
        {
            StopCoroutine(roundSequenceRoutine);
            roundSequenceRoutine = null;
        }
    }

    public void ShowRevealing()
    {
        roundText.text = "Revealing Cards";
    }

}

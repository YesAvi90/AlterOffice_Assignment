using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    public static ScoreUI Instance;

    [SerializeField] private TextMeshProUGUI playerScore;
    [SerializeField] private TextMeshProUGUI opponentScore;

    void Awake() => Instance = this;

    public void UpdateScores(int p1, int p2)
    {
        if (PlayerIdentity.LocalPlayerId == "P1")
        {
            playerScore.text = "Your Score: " + p1.ToString();
            opponentScore.text = "Opponent Score: " +p2.ToString();
        }
        else
        {
            playerScore.text = "Your Score: " + p2.ToString();
            opponentScore.text = "Opponent Score: " + p1.ToString();
        }
    }
}

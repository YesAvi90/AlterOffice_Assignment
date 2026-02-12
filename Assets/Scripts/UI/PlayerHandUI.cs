using UnityEngine;

public class PlayerHandUI : MonoBehaviour
{
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private CardView cardPrefab;

    [Header("Scroll Rect")]
    [SerializeField] private Transform contentRoot; 

    public static PlayerHandUI Instance;

    void Awake() => Instance = this;

    public void BuildHand()
    {
        if (playerHand == null || contentRoot == null)
        {
            Debug.LogError("PlayerHandUI not wired correctly");
            return;
        }

        // Clear existing cards
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        // Spawn cards inside ScrollRect content
        foreach (var card in playerHand.hand)
        {
            CardView view = Instantiate(cardPrefab, contentRoot);
            view.Bind(card, playerHand);
        }
    }
}

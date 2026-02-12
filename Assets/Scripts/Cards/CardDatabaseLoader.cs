using UnityEngine;

public class CardDatabaseLoader : MonoBehaviour
{
    public static CardDatabaseLoader Instance;

    public CardData[] Cards { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCards();
    }

    void LoadCards()
    {
        TextAsset json = Resources.Load<TextAsset>("Cards/cards");
        if (json == null)
        {
            Debug.LogError("Failed to load cards.json");
            return;
        }

        CardDatabase database =
            JsonUtility.FromJson<CardDatabase>(json.text);

        Cards = database.cards;

        Debug.Log($"Loaded {Cards.Length} cards");
    }

    public CardData GetCardById(int id)
    {
        foreach (var card in Cards)
            if (card.id == id)
                return card;

        return null;
    }
}

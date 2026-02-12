using System.Collections.Generic;
using UnityEngine;

public class OpponentPlayedCardsUI : MonoBehaviour
{
    public static OpponentPlayedCardsUI Instance;
    [SerializeField] private CardView cardPrefab;

    private readonly List<CardView> allViews = new();
    private int currentTurnStartIndex = 0;

    void Awake() => Instance = this;

    public void AddFoldedCards(List<PlayedCard> cards)
    {
        currentTurnStartIndex = allViews.Count;

        foreach (var _ in cards)
        {
            CardView view = Instantiate(cardPrefab, transform);
            view.SetFaceDown(true);

            allViews.Add(view);
        }
    }

    public void RevealCardAt(int order, CardData data)
    {
        int realIndex = currentTurnStartIndex + order;

        if (realIndex < 0 || realIndex >= allViews.Count)
            return;

        CardView view = allViews[realIndex];

        view.BindReadOnly(data);
        view.SetFaceDown(false);
    }

    public void ClearAll()
    {
        allViews.Clear();
        currentTurnStartIndex = 0;

        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }

    public void RemoveCardById(int cardId)
    {
        foreach (Transform child in transform)
        {
            CardView view = child.GetComponent<CardView>();
            if (view != null && view.CardId == cardId)
            {
                Destroy(child.gameObject);
                break;
            }
        }
    }


}

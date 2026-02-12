using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    public List<CardInstance> hand = new();
    public List<CardInstance> selected = new();
    public List<PlayedCard> playedThisTurn;

    public int AvailableCost { get; set; }

    public static PlayerHand Instance;

    void Awake() => Instance = this;


    public void InitHand(List<CardData> startingCards)
    {
        hand.Clear();
        selected.Clear();

        foreach (var data in startingCards)
        {
            hand.Add(new CardInstance(data));
        }
    }


    public bool ToggleSelect(CardInstance card)
    {
        if (card.IsSelected)
        {
            card.IsSelected = false;
            selected.Remove(card);

            EnergyUI.Instance.UpdateEnergy(
                AvailableCost - GetSelectedCost()
            );

            return true;
        }

        int newCost = GetSelectedCost() + card.Cost;
        if (newCost > AvailableCost)
            return false;

        card.IsSelected = true;
        selected.Add(card);

        EnergyUI.Instance.UpdateEnergy(
            AvailableCost - GetSelectedCost()
        );

        return true;
    }



    public int GetSelectedCost()
    {
        int sum = 0;
        foreach (var c in selected)
            sum += c.Cost;
        return sum;
    }

    public List<CardInstance> GetSelectedCards()
    {
        return new List<CardInstance>(selected);
    }



    public EndTurnPayload BuildEndTurnPayload()
    {
        EndTurnPayload payload = new EndTurnPayload
        {
            playerId = PlayerIdentity.LocalPlayerId,
            cards = new List<PlayerCardData>()
        };

        foreach (var pc in playedThisTurn)
        {
            payload.cards.Add(new PlayerCardData
            {
                cardId = pc.cardId,
                order = pc.order
            });
        }

        return payload;
    }

    public void ClearPlayedThisTurn()
    {
        playedThisTurn.Clear();
    }

    public void SetAvailableCost(int cost)
    {
        AvailableCost = cost;
        Debug.Log($"[CLIENT] Available energy set to {cost}");
    }



}

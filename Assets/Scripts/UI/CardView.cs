using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI abilityText;

    private CardInstance boundCard;
    private PlayerHand ownerHand;

    private Color normalColor = Color.white;
    private Color selectedColor = new Color(0.7f, 0.9f, 1f);

    private bool isFaceDown = false;


    public int CardId { get; private set; }

    public void Bind(CardInstance card, PlayerHand hand)
    {
        boundCard = card;
        ownerHand = hand;


        CardId = card.Id;

        nameText.text = card.Name;
        costText.text = $"Cost: {card.Cost}";
        powerText.text = $"Power: {card.Power}";

        if (card.Data != null && card.Data.ability != null)
            abilityText.text = FormatAbility(card.Data.ability.type);
        else
            abilityText.text = "";

        RefreshVisual();
    }

    public void OnClick()
    {
        if (boundCard == null || ownerHand == null)
            return;

        bool success = ownerHand.ToggleSelect(boundCard);
        if (!success)
        {
            Debug.Log("Not enough cost to select card");
            return;
        }

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        background.color = boundCard != null && boundCard.IsSelected
            ? selectedColor
            : normalColor;
    }

    public void BindReadOnly(CardData data)
    {
        boundCard = null;
        ownerHand = null;


        CardId = data.id;

        nameText.text = data.name;
        costText.text = $"Cost: {data.cost}";
        powerText.text = $"Power: {data.power}";

        if (data.ability != null)
            abilityText.text = FormatAbility(data.ability.type);
        else
            abilityText.text = "";

        background.color = Color.white;
    }

    private string FormatAbility(string type)
    {
        switch (type)
        {
            case "StealPoints":
                return "Steal Points";

            case "BonusPoints":
                return "Bonus Points";

            case "DrawCard":
                return "Draw Card";

            case "DiscardOpponentCard":
                return "Discard Opponent Card";

            default:
                return "";
        }
    }

    public void SetFaceDown(bool value)
    {
        if (value)
        {
            powerText.text = "?";
            nameText.text = "";
            costText.text = "";
            abilityText.text = "";
        }
    }
}

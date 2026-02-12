using System.Collections.Generic;

public static class StartingHandCache
{
    private static List<CardData> cachedCards;
    private static bool gameStartSequenceFinished;

    public static void Cache(List<CardData> cards)
    {
        cachedCards = cards;
        TryApply();
    }

    public static void NotifyGameStartSequenceFinished()
    {
        gameStartSequenceFinished = true;
        TryApply();
    }

    private static void TryApply()
    {
        if (!gameStartSequenceFinished)
            return;

        if (cachedCards == null || cachedCards.Count == 0)
            return;

        PlayerHand.Instance.InitHand(cachedCards);
        PlayerHandUI.Instance.BuildHand();

        cachedCards = null;
    }

    public static void Reset()
    {
        cachedCards = null;
        gameStartSequenceFinished = false;
    }
}

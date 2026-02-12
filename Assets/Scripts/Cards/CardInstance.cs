public class CardInstance
{
    public CardData Data { get; }
    public bool IsSelected { get; set; }

    public CardInstance(CardData data)
    {
        Data = data;
        IsSelected = false;
    }

    public int Cost => Data.cost;
    public int Power => Data.power;
    public int Id => Data.id;
    public string Name => Data.name;
}

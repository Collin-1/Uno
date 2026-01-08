namespace UnoGame.Models;

public enum CardColor
{
    Red,
    Blue,
    Green,
    Yellow,
    Wild
}

public enum CardType
{
    Number,
    Skip,
    Reverse,
    DrawTwo,
    Wild,
    WildDrawFour
}

public class Card
{
    public CardColor Color { get; set; }
    public CardType Type { get; set; }
    public int? Number { get; set; } // Null for non-number cards
    public string Id { get; set; } // Unique identifier for each card instance

    public Card(CardColor color, CardType type, int? number = null)
    {
        Color = color;
        Type = type;
        Number = number;
        Id = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Gets the filename for this card's image asset
    /// </summary>
    public string GetImageFileName()
    {
        return Type switch
        {
            CardType.Number => $"{Color.ToString().ToLower()}_{Number}.png",
            CardType.Skip => $"{Color.ToString().ToLower()}_skip.png",
            CardType.Reverse => $"{Color.ToString().ToLower()}_reverse.png",
            CardType.DrawTwo => $"{Color.ToString().ToLower()}_draw_two.png",
            CardType.Wild => "wild.png",
            CardType.WildDrawFour => "wild_draw_four.png",
            _ => "card_back.png"
        };
    }

    /// <summary>
    /// Checks if this card can be played on top of another card
    /// </summary>
    public bool CanPlayOn(Card topCard, CardColor? currentColor = null)
    {
        // Wild cards can always be played
        if (Type == CardType.Wild || Type == CardType.WildDrawFour)
        {
            return true;
        }

        // Use the selected color if available (after a wild card)
        var effectiveColor = currentColor ?? topCard.Color;

        // Match by color or by type/number
        return Color == effectiveColor ||
               (Type == topCard.Type && Type != CardType.Number) ||
               (Number.HasValue && Number == topCard.Number);
    }

    public override string ToString()
    {
        return Type == CardType.Number
            ? $"{Color} {Number}"
            : $"{Color} {Type}";
    }
}

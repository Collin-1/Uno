namespace UnoGame.Models;

public class Player
{
    public string ConnectionId { get; set; }
    public string Name { get; set; }
    public List<Card> Hand { get; set; }
    public bool IsReady { get; set; }

    public Player(string connectionId, string name)
    {
        ConnectionId = connectionId;
        Name = name;
        Hand = new List<Card>();
        IsReady = false;
    }

    public int CardCount => Hand.Count;

    /// <summary>
    /// Removes a card from the player's hand
    /// </summary>
    public bool RemoveCard(string cardId)
    {
        var card = Hand.FirstOrDefault(c => c.Id == cardId);
        if (card != null)
        {
            Hand.Remove(card);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a player's hand summary (for other players - just count)
    /// </summary>
    public object GetPublicInfo()
    {
        return new
        {
            Name,
            CardCount
        };
    }

    /// <summary>
    /// Gets full player info including cards (for the player themselves)
    /// </summary>
    public object GetPrivateInfo()
    {
        return new
        {
            Name,
            CardCount,
            Hand = Hand.Select(c => new
            {
                c.Id,
                c.Color,
                c.Type,
                c.Number,
                ImageFile = c.GetImageFileName()
            })
        };
    }
}

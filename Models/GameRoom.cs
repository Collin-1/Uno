namespace UnoGame.Models;

public enum GameDirection
{
    Clockwise,
    CounterClockwise
}

public enum GameStatus
{
    Waiting,
    InProgress,
    Finished
}

public class GameRoom
{
    public string RoomId { get; set; }
    public string RoomName { get; set; }
    public List<Player> Players { get; set; }
    public List<Card> Deck { get; set; }
    public List<Card> DiscardPile { get; set; }
    public int CurrentPlayerIndex { get; set; }
    public GameDirection Direction { get; set; }
    public GameStatus Status { get; set; }
    public CardColor? CurrentWildColor { get; set; } // Color selected when wild card is played
    public int MaxPlayers { get; set; }
    public DateTime CreatedAt { get; set; }

    public GameRoom(string roomName, int maxPlayers = 6)
    {
        RoomId = Guid.NewGuid().ToString();
        RoomName = roomName;
        Players = new List<Player>();
        Deck = new List<Card>();
        DiscardPile = new List<Card>();
        CurrentPlayerIndex = 0;
        Direction = GameDirection.Clockwise;
        Status = GameStatus.Waiting;
        MaxPlayers = Math.Clamp(maxPlayers, 2, 6);
        CreatedAt = DateTime.UtcNow;
    }

    public Player? CurrentPlayer => Players.Count > 0 && CurrentPlayerIndex < Players.Count
        ? Players[CurrentPlayerIndex]
        : null;

    public Card? TopCard => DiscardPile.Count > 0
        ? DiscardPile[^1]
        : null;

    public bool IsFull => Players.Count >= MaxPlayers;

    public bool CanStart => Players.Count >= 2 && Status == GameStatus.Waiting;

    /// <summary>
    /// Advances to the next player based on current direction
    /// </summary>
    public void NextTurn()
    {
        if (Direction == GameDirection.Clockwise)
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        }
        else
        {
            CurrentPlayerIndex = (CurrentPlayerIndex - 1 + Players.Count) % Players.Count;
        }
    }

    /// <summary>
    /// Reverses the direction of play
    /// </summary>
    public void ReverseDirection()
    {
        Direction = Direction == GameDirection.Clockwise
            ? GameDirection.CounterClockwise
            : GameDirection.Clockwise;
    }

    /// <summary>
    /// Gets the next player without advancing the turn
    /// </summary>
    public Player? PeekNextPlayer()
    {
        if (Players.Count == 0) return null;

        int nextIndex;
        if (Direction == GameDirection.Clockwise)
        {
            nextIndex = (CurrentPlayerIndex + 1) % Players.Count;
        }
        else
        {
            nextIndex = (CurrentPlayerIndex - 1 + Players.Count) % Players.Count;
        }

        return Players[nextIndex];
    }

    /// <summary>
    /// Finds a player by their connection ID
    /// </summary>
    public Player? GetPlayerByConnectionId(string connectionId)
    {
        return Players.FirstOrDefault(p => p.ConnectionId == connectionId);
    }

    /// <summary>
    /// Gets the game state for broadcasting to all players
    /// </summary>
    public object GetGameState()
    {
        return new
        {
            RoomId,
            RoomName,
            Status = Status.ToString(),
            Direction = Direction.ToString(),
            CurrentPlayerIndex,
            CurrentPlayerName = CurrentPlayer?.Name,
            TopCard = TopCard != null ? new
            {
                TopCard.Color,
                TopCard.Type,
                TopCard.Number,
                ImageFile = TopCard.GetImageFileName()
            } : null,
            CurrentWildColor = CurrentWildColor?.ToString(),
            Players = Players.Select((p, index) => new
            {
                p.ConnectionId,
                p.Name,
                p.CardCount,
                IsCurrentPlayer = index == CurrentPlayerIndex
            }),
            DeckCount = Deck.Count
        };
    }
}

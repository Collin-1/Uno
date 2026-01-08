using UnoGame.Models;
using System.Collections.Concurrent;

namespace UnoGame.Services;

public class GameService
{
    // Thread-safe dictionary to store all active game rooms
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms = new();

    /// <summary>
    /// Creates a new game room
    /// </summary>
    public GameRoom CreateRoom(string roomName, int maxPlayers = 6)
    {
        var room = new GameRoom(roomName, maxPlayers);
        _gameRooms.TryAdd(room.RoomId, room);
        return room;
    }

    /// <summary>
    /// Gets a game room by ID
    /// </summary>
    public GameRoom? GetRoom(string roomId)
    {
        _gameRooms.TryGetValue(roomId, out var room);
        return room;
    }

    /// <summary>
    /// Gets all available game rooms
    /// </summary>
    public List<GameRoom> GetAvailableRooms()
    {
        return _gameRooms.Values
            .Where(r => r.Status == GameStatus.Waiting && !r.IsFull)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Gets all game rooms (for finding disconnected players)
    /// </summary>
    public List<GameRoom> GetAllRooms()
    {
        return _gameRooms.Values.ToList();
    }

    /// <summary>
    /// Adds a player to a game room
    /// </summary>
    public bool AddPlayerToRoom(string roomId, Player player)
    {
        var room = GetRoom(roomId);
        if (room == null || room.IsFull || room.Status != GameStatus.Waiting)
        {
            return false;
        }

        room.Players.Add(player);
        return true;
    }

    /// <summary>
    /// Removes a player from a game room
    /// </summary>
    public bool RemovePlayerFromRoom(string roomId, string connectionId)
    {
        var room = GetRoom(roomId);
        if (room == null) return false;

        var player = room.GetPlayerByConnectionId(connectionId);
        if (player == null) return false;

        room.Players.Remove(player);

        // If game is in progress and fewer than 2 players remain, end the game
        if (room.Status == GameStatus.InProgress && room.Players.Count < 2)
        {
            room.Status = GameStatus.Finished;
        }

        // Remove empty rooms
        if (room.Players.Count == 0)
        {
            _gameRooms.TryRemove(roomId, out _);
        }

        return true;
    }

    /// <summary>
    /// Creates a standard UNO deck (108 cards)
    /// </summary>
    public List<Card> CreateDeck()
    {
        var deck = new List<Card>();

        // For each color (Red, Blue, Green, Yellow)
        foreach (CardColor color in new[] { CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow })
        {
            // One 0 card per color
            deck.Add(new Card(color, CardType.Number, 0));

            // Two of each number 1-9 per color
            for (int num = 1; num <= 9; num++)
            {
                deck.Add(new Card(color, CardType.Number, num));
                deck.Add(new Card(color, CardType.Number, num));
            }

            // Two Skip cards per color
            deck.Add(new Card(color, CardType.Skip));
            deck.Add(new Card(color, CardType.Skip));

            // Two Reverse cards per color
            deck.Add(new Card(color, CardType.Reverse));
            deck.Add(new Card(color, CardType.Reverse));

            // Two Draw Two cards per color
            deck.Add(new Card(color, CardType.DrawTwo));
            deck.Add(new Card(color, CardType.DrawTwo));
        }

        // Four Wild cards
        for (int i = 0; i < 4; i++)
        {
            deck.Add(new Card(CardColor.Wild, CardType.Wild));
        }

        // Four Wild Draw Four cards
        for (int i = 0; i < 4; i++)
        {
            deck.Add(new Card(CardColor.Wild, CardType.WildDrawFour));
        }

        return deck;
    }

    /// <summary>
    /// Shuffles a deck using Fisher-Yates algorithm
    /// </summary>
    public void ShuffleDeck(List<Card> deck)
    {
        var random = new Random();
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }

    /// <summary>
    /// Starts a game - deals cards and sets up initial state
    /// </summary>
    public void StartGame(string roomId)
    {
        var room = GetRoom(roomId);
        if (room == null || !room.CanStart) return;

        // Create and shuffle deck
        room.Deck = CreateDeck();
        ShuffleDeck(room.Deck);

        // Deal 7 cards to each player
        foreach (var player in room.Players)
        {
            player.Hand.Clear();
            for (int i = 0; i < 7; i++)
            {
                if (room.Deck.Count > 0)
                {
                    player.Hand.Add(room.Deck[0]);
                    room.Deck.RemoveAt(0);
                }
            }
        }

        // Find first non-wild card for starting card
        Card? startCard = null;
        for (int i = 0; i < room.Deck.Count; i++)
        {
            if (room.Deck[i].Type != CardType.Wild && room.Deck[i].Type != CardType.WildDrawFour)
            {
                startCard = room.Deck[i];
                room.Deck.RemoveAt(i);
                break;
            }
        }

        if (startCard != null)
        {
            room.DiscardPile.Add(startCard);
        }

        room.Status = GameStatus.InProgress;
        room.CurrentPlayerIndex = 0;
        room.Direction = GameDirection.Clockwise;
        room.CurrentWildColor = null;
    }

    /// <summary>
    /// Validates if a card can be played
    /// </summary>
    public (bool isValid, string? errorMessage) ValidateCardPlay(GameRoom room, Player player, string cardId)
    {
        // Check if it's the player's turn
        if (room.CurrentPlayer?.ConnectionId != player.ConnectionId)
        {
            return (false, "It's not your turn");
        }

        // Find the card in player's hand
        var card = player.Hand.FirstOrDefault(c => c.Id == cardId);
        if (card == null)
        {
            return (false, "Card not found in your hand");
        }

        var topCard = room.TopCard;
        if (topCard == null)
        {
            return (false, "No top card");
        }

        // Check if card can be played on top card
        if (!card.CanPlayOn(topCard, room.CurrentWildColor))
        {
            // Wild Draw Four can only be played if player has no cards matching current color
            if (card.Type == CardType.WildDrawFour)
            {
                var effectiveColor = room.CurrentWildColor ?? topCard.Color;
                var hasMatchingColor = player.Hand.Any(c =>
                    c.Id != cardId && c.Color == effectiveColor && c.Type != CardType.WildDrawFour);

                if (hasMatchingColor)
                {
                    return (false, "Wild Draw Four can only be played when you have no cards of the current color");
                }
            }
            else
            {
                return (false, "Card cannot be played on the current top card");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Plays a card from a player's hand
    /// </summary>
    public (bool success, string? message) PlayCard(string roomId, string connectionId, string cardId, CardColor? wildColor = null)
    {
        var room = GetRoom(roomId);
        if (room == null) return (false, "Room not found");

        var player = room.GetPlayerByConnectionId(connectionId);
        if (player == null) return (false, "Player not found");

        var card = player.Hand.FirstOrDefault(c => c.Id == cardId);
        if (card == null) return (false, "Card not found");

        // Validate the play
        var (isValid, errorMessage) = ValidateCardPlay(room, player, cardId);
        if (!isValid) return (false, errorMessage);

        // Remove card from player's hand and add to discard pile
        player.RemoveCard(cardId);
        room.DiscardPile.Add(card);

        // Handle wild color selection
        if (card.Type == CardType.Wild || card.Type == CardType.WildDrawFour)
        {
            if (wildColor == null || wildColor == CardColor.Wild)
            {
                return (false, "Must select a color for wild card");
            }
            room.CurrentWildColor = wildColor;
        }
        else
        {
            room.CurrentWildColor = null;
        }

        // Check for win condition
        if (player.CardCount == 0)
        {
            room.Status = GameStatus.Finished;
            return (true, $"{player.Name} wins!");
        }

        // Apply card effects
        ApplyCardEffect(room, card);

        return (true, null);
    }

    /// <summary>
    /// Applies special card effects (Skip, Reverse, Draw Two, Wild Draw Four)
    /// </summary>
    private void ApplyCardEffect(GameRoom room, Card card)
    {
        switch (card.Type)
        {
            case CardType.Skip:
                // Skip next player
                room.NextTurn();
                room.NextTurn();
                break;

            case CardType.Reverse:
                // Reverse direction
                room.ReverseDirection();
                // In 2-player game, reverse acts like skip
                if (room.Players.Count == 2)
                {
                    room.NextTurn();
                }
                room.NextTurn();
                break;

            case CardType.DrawTwo:
                // Next player draws 2 cards
                room.NextTurn();
                DrawCards(room, room.CurrentPlayer!, 2);
                room.NextTurn();
                break;

            case CardType.WildDrawFour:
                // Next player draws 4 cards
                room.NextTurn();
                DrawCards(room, room.CurrentPlayer!, 4);
                room.NextTurn();
                break;

            default:
                // Normal card - just advance turn
                room.NextTurn();
                break;
        }
    }

    /// <summary>
    /// Draws cards from the deck for a player
    /// </summary>
    public List<Card> DrawCards(GameRoom room, Player player, int count)
    {
        var drawnCards = new List<Card>();

        for (int i = 0; i < count; i++)
        {
            // If deck is empty, reshuffle discard pile (except top card)
            if (room.Deck.Count == 0)
            {
                ReshuffleDiscardPile(room);
            }

            // Draw card if available
            if (room.Deck.Count > 0)
            {
                var card = room.Deck[0];
                room.Deck.RemoveAt(0);
                player.Hand.Add(card);
                drawnCards.Add(card);
            }
        }

        return drawnCards;
    }

    /// <summary>
    /// Reshuffles the discard pile back into the deck when deck runs out
    /// </summary>
    private void ReshuffleDiscardPile(GameRoom room)
    {
        if (room.DiscardPile.Count <= 1) return;

        // Keep the top card, shuffle the rest back into deck
        var topCard = room.DiscardPile[^1];
        room.DiscardPile.RemoveAt(room.DiscardPile.Count - 1);

        room.Deck.AddRange(room.DiscardPile);
        room.DiscardPile.Clear();
        room.DiscardPile.Add(topCard);

        ShuffleDeck(room.Deck);
    }

    /// <summary>
    /// Handles when a player draws a card on their turn
    /// </summary>
    public (bool success, List<Card> drawnCards) PlayerDrawCard(string roomId, string connectionId)
    {
        var room = GetRoom(roomId);
        if (room == null) return (false, new List<Card>());

        var player = room.GetPlayerByConnectionId(connectionId);
        if (player == null) return (false, new List<Card>());

        // Check if it's the player's turn
        if (room.CurrentPlayer?.ConnectionId != player.ConnectionId)
        {
            return (false, new List<Card>());
        }

        // Draw one card
        var drawnCards = DrawCards(room, player, 1);

        // Advance turn after drawing
        room.NextTurn();

        return (true, drawnCards);
    }
}

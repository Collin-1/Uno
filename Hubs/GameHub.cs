using Microsoft.AspNetCore.SignalR;
using UnoGame.Models;
using UnoGame.Services;

namespace UnoGame.Hubs;

public class GameHub : Hub
{
    private readonly GameService _gameService;

    public GameHub(GameService gameService)
    {
        _gameService = gameService;
    }

    /// <summary>
    /// Player joins a game room
    /// </summary>
    public async Task JoinRoom(string roomId, string playerName)
    {
        var room = _gameService.GetRoom(roomId);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "Room not found");
            return;
        }

        if (room.IsFull)
        {
            await Clients.Caller.SendAsync("Error", "Room is full");
            return;
        }

        if (room.Status != GameStatus.Waiting)
        {
            await Clients.Caller.SendAsync("Error", "Game already in progress");
            return;
        }

        // Create new player
        var player = new Player(Context.ConnectionId, playerName);

        if (!_gameService.AddPlayerToRoom(roomId, player))
        {
            await Clients.Caller.SendAsync("Error", "Could not join room");
            return;
        }

        // Add to SignalR group
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Notify caller of successful join
        await Clients.Caller.SendAsync("JoinedRoom", roomId, playerName);

        // Broadcast updated player list to all players in room
        await BroadcastGameState(roomId);

        // Notify all players that a new player joined
        await Clients.Group(roomId).SendAsync("PlayerJoined", playerName, room.Players.Count);
    }

    /// <summary>
    /// Starts the game
    /// </summary>
    public async Task StartGame(string roomId)
    {
        var room = _gameService.GetRoom(roomId);
        if (room == null || !room.CanStart)
        {
            await Clients.Caller.SendAsync("Error", "Cannot start game");
            return;
        }

        _gameService.StartGame(roomId);

        // Notify all players that game has started
        await Clients.Group(roomId).SendAsync("GameStarted");

        // Send game state to all players
        await BroadcastGameState(roomId);

        // Send private hands to each player
        await SendPrivateHands(roomId);
    }

    /// <summary>
    /// Player plays a card
    /// </summary>
    public async Task PlayCard(string roomId, string cardId, string? wildColor)
    {
        var room = _gameService.GetRoom(roomId);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "Room not found");
            return;
        }

        var player = room.GetPlayerByConnectionId(Context.ConnectionId);
        if (player == null)
        {
            await Clients.Caller.SendAsync("Error", "Player not found");
            return;
        }

        // Parse wild color if provided (case-insensitive)
        CardColor? selectedColor = null;
        if (!string.IsNullOrEmpty(wildColor) && Enum.TryParse<CardColor>(wildColor, ignoreCase: true, out var color))
        {
            selectedColor = color;
        }

        // Attempt to play the card
        var (success, message) = _gameService.PlayCard(roomId, Context.ConnectionId, cardId, selectedColor);

        if (!success)
        {
            await Clients.Caller.SendAsync("Error", message ?? "Could not play card");
            return;
        }

        // Check if game is finished
        if (room.Status == GameStatus.Finished)
        {
            await Clients.Group(roomId).SendAsync("GameOver", player.Name);
            await BroadcastGameState(roomId);
            return;
        }

        // Broadcast updated game state
        await BroadcastGameState(roomId);
        await SendPrivateHands(roomId);

        // Notify about the card played
        var card = room.TopCard;
        await Clients.Group(roomId).SendAsync("CardPlayed", player.Name, new
        {
            card?.Color,
            card?.Type,
            card?.Number,
            ImageFile = card?.GetImageFileName()
        });
    }

    /// <summary>
    /// Player draws a card
    /// </summary>
    public async Task DrawCard(string roomId)
    {
        var room = _gameService.GetRoom(roomId);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "Room not found");
            return;
        }

        var player = room.GetPlayerByConnectionId(Context.ConnectionId);
        if (player == null)
        {
            await Clients.Caller.SendAsync("Error", "Player not found");
            return;
        }

        var (success, drawnCards) = _gameService.PlayerDrawCard(roomId, Context.ConnectionId);

        if (!success)
        {
            await Clients.Caller.SendAsync("Error", "Could not draw card");
            return;
        }

        // Send drawn cards to player
        await Clients.Caller.SendAsync("CardsDrawn", drawnCards.Select(c => new
        {
            c.Id,
            c.Color,
            c.Type,
            c.Number,
            ImageFile = c.GetImageFileName()
        }));

        // Broadcast updated game state
        await BroadcastGameState(roomId);
        await SendPrivateHands(roomId);

        // Notify all players about the draw
        await Clients.Group(roomId).SendAsync("PlayerDrew", player.Name, drawnCards.Count);
    }

    /// <summary>
    /// Player leaves the room
    /// </summary>
    public async Task LeaveRoom(string roomId)
    {
        await HandlePlayerLeaving(roomId, Context.ConnectionId);
    }

    /// <summary>
    /// Handle disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Find which room this player was in and remove them
        var rooms = _gameService.GetAllRooms();
        foreach (var room in rooms)
        {
            if (room.GetPlayerByConnectionId(Context.ConnectionId) != null)
            {
                await HandlePlayerLeaving(room.RoomId, Context.ConnectionId);
                break;
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Broadcasts the current game state to all players in a room
    /// </summary>
    private async Task BroadcastGameState(string roomId)
    {
        var room = _gameService.GetRoom(roomId);
        if (room == null) return;

        var gameState = room.GetGameState();
        await Clients.Group(roomId).SendAsync("GameStateUpdated", gameState);
    }

    /// <summary>
    /// Sends each player their private hand
    /// </summary>
    private async Task SendPrivateHands(string roomId)
    {
        var room = _gameService.GetRoom(roomId);
        if (room == null) return;

        foreach (var player in room.Players)
        {
            await Clients.Client(player.ConnectionId).SendAsync("UpdateHand", player.GetPrivateInfo());
        }
    }

    /// <summary>
    /// Handles a player leaving the room
    /// </summary>
    private async Task HandlePlayerLeaving(string roomId, string connectionId)
    {
        var room = _gameService.GetRoom(roomId);
        if (room == null) return;

        var player = room.GetPlayerByConnectionId(connectionId);
        if (player == null) return;

        var playerName = player.Name;

        // Remove player from room
        _gameService.RemovePlayerFromRoom(roomId, connectionId);

        // Remove from SignalR group
        await Groups.RemoveFromGroupAsync(connectionId, roomId);

        // Notify remaining players
        await Clients.Group(roomId).SendAsync("PlayerLeft", playerName);

        // If game ended due to insufficient players
        if (room.Status == GameStatus.Finished)
        {
            await Clients.Group(roomId).SendAsync("GameOver", "Game ended - not enough players");
        }
        else
        {
            // Update game state for remaining players
            await BroadcastGameState(roomId);
        }
    }

    /// <summary>
    /// Gets list of available rooms
    /// </summary>
    public async Task GetAvailableRooms()
    {
        var rooms = _gameService.GetAvailableRooms();
        var roomList = rooms.Select(r => new
        {
            r.RoomId,
            r.RoomName,
            PlayerCount = r.Players.Count,
            r.MaxPlayers,
            Status = r.Status.ToString()
        });

        await Clients.Caller.SendAsync("AvailableRooms", roomList);
    }

    // WebRTC Signaling
    public async Task SendWebRTCOffer(string roomId, string targetConnectionId, string offer)
    {
        await Clients.Client(targetConnectionId).SendAsync("ReceiveWebRTCOffer", Context.ConnectionId, offer);
    }

    public async Task SendWebRTCAnswer(string roomId, string targetConnectionId, string answer)
    {
        await Clients.Client(targetConnectionId).SendAsync("ReceiveWebRTCAnswer", Context.ConnectionId, answer);
    }

    public async Task SendICECandidate(string roomId, string targetConnectionId, string candidate)
    {
        await Clients.Client(targetConnectionId).SendAsync("ReceiveICECandidate", Context.ConnectionId, candidate);
    }
}

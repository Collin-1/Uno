using Microsoft.AspNetCore.Mvc;
using UnoGame.Services;

namespace UnoGame.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly GameService _gameService;

    public RoomsController(GameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet]
    public IActionResult GetAvailableRooms()
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

        return Ok(roomList);
    }

    [HttpPost("create")]
    public IActionResult CreateRoom([FromBody] CreateRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoomName))
        {
            return BadRequest(new { error = "Room name is required" });
        }

        var room = _gameService.CreateRoom(request.RoomName, request.MaxPlayers);

        return Ok(new { roomId = room.RoomId, roomName = room.RoomName });
    }
}

public class CreateRoomRequest
{
    public string RoomName { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = 4;
}

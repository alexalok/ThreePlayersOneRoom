using Microsoft.AspNetCore.Mvc;
using ThreePlayersOneRoom.BackgroundWorks;
using ThreePlayersOneRoom.Exceptions;
using ThreePlayersOneRoom.Repositories;

namespace ThreePlayersOneRoom.Controllers;

[ApiController]
[Route("/[controller]")]
public class RoomsController : ControllerBase
{
    readonly IRoomsRepository _rooms;
    readonly ISessionRequester _sessionsHandler;

    Guid UserId => Guid.Parse(User.Identity!.Name!); // we require authentication

    public RoomsController(IRoomsRepository rooms, ISessionRequester sessionsHandler)
    {
        _rooms = rooms;
        _sessionsHandler = sessionsHandler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom()
    {
        var roomId = await _rooms.CreateRoom(UserId);
        return Ok(roomId);
    }

    [HttpPost("{roomId:int}/join")]
    public async Task<IActionResult> JoinRoom(int roomId)
    {
        try
        {
            await _rooms.JoinRoom(roomId, UserId);
            _sessionsHandler.RequestSessionForRoom(roomId);
            return Ok();
        }
        catch (RoomNotFoundException)
        {
            return NotFound();
        }
        catch (RoomIsFullException)
        {
            return Conflict("Room is full.");
        }
        catch (TryingToJoinOwnRoomException)
        {
            return Conflict("Joining own room is prohibited.");
        }
    }
}
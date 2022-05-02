using Microsoft.EntityFrameworkCore;
using ThreePlayersOneRoom.DB;
using ThreePlayersOneRoom.DB.Entities;
using ThreePlayersOneRoom.Exceptions;

namespace ThreePlayersOneRoom.Repositories;

public interface IRoomsRepository
{
    Task<int> CreateRoom(Guid hostId);

    Task JoinRoom(int roomId, Guid followerId);
    Task<Room> GetRoom(int roomId);
    Task SetWinner(int roomId, Guid winnerId);
}

public class RoomsRepository : IRoomsRepository
{
    readonly GameDbContext _db;

    public RoomsRepository(GameDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateRoom(Guid hostId)
    {
        Room room = new()
        {
            HostId = hostId,
        };
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        return room.Id;
    }

    /// <exception cref="RoomNotFoundException"></exception>
    /// <exception cref="RoomIsFullException"></exception>
    /// <exception cref="TryingToJoinOwnRoomException"></exception>
    public async Task JoinRoom(int roomId, Guid followerId)
    {
        var room = await _db.Rooms.FindAsync(roomId);
        if (room == null)
            throw new RoomNotFoundException();

        if (room.HostId == followerId)
            throw new TryingToJoinOwnRoomException();

        if (room.FollowerId != null)
            throw new RoomIsFullException();

        room.FollowerId = followerId;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RoomIsFullException();
        }
    }

    public async Task<Room> GetRoom(int roomId)
    {
        return await _db.Rooms.FindAsync(roomId) ?? throw new RoomNotFoundException();
    }

    public async Task SetWinner(int roomId, Guid winnerId)
    {
        var room = await _db.Rooms.FindAsync(roomId);
        if (room == null)
            throw new RoomNotFoundException();

        if (room.Outcome != null)
            throw new InvalidOperationException("Room has already concluded.");

        if (winnerId == room.HostId)
            room.Outcome = Outcome.HostWon;
        else if (winnerId == room.FollowerId)
            room.Outcome = Outcome.FollowerWon;
        else
            throw new ArgumentException("User with the given ID does not belong to this room.");

        await _db.SaveChangesAsync();
    }
}
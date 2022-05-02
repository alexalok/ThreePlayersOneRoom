using System.Diagnostics;
using ThreePlayersOneRoom.DB.Entities;
using ThreePlayersOneRoom.Factories;
using ThreePlayersOneRoom.Models;
using ThreePlayersOneRoom.Services.SSE;

namespace ThreePlayersOneRoom.Handlers;

public interface ISessionHandler
{
    Task HandleSession(int roomId);
}

public class SessionHandler : ISessionHandler
{
    readonly ILogger<SessionHandler> _logger;
    readonly IRoomsRepositoryFactory _roomsFac;
    readonly IPlayersNotifier _notifier;

    public SessionHandler(ILogger<SessionHandler> logger, IRoomsRepositoryFactory roomsFac,
        IPlayersNotifier notifier)
    {
        _logger = logger;
        _roomsFac = roomsFac;
        _notifier = notifier;
    }

    public async Task HandleSession(int roomId)
    {
        var session = await RunSessionToEnd(roomId);
        await SaveSessionResults(roomId, session);

        Debug.Assert(!session.CanAdvance);
        await NotifyClientsOfResults(roomId, session.WinnerId.Value, session.LoserId.Value);
    }

    async Task<Session> RunSessionToEnd(int roomId)
    {
        Room room;
        await using (var _ = _roomsFac.Create(out var rooms))
            room = await rooms.GetRoom(roomId);

        if (room.FollowerId == null)
            throw new InvalidOperationException("Trying to run a game with no follower.");
        Session session = new(room.HostId, room.FollowerId.Value);
        _logger.LogDebug("Created session for room {RoomId}", room.Id);

        while (session.CanAdvance)
        {
            session.Advance(Random.Shared.Next(0, 3), Random.Shared.Next(0, 3));
            _logger.LogTrace("Advanced session of room {RoomId}", room.Id);
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        _logger.LogInformation("Session of room {RoomId} ended. Winner: {WinnerId}",
            room.Id, session.WinnerId);

        return session;
    }

    async Task SaveSessionResults(int roomId, Session session)
    {
        await using var _ = _roomsFac.Create(out var rooms);
        Debug.Assert(!session.CanAdvance);
        await rooms.SetWinner(roomId, session.WinnerId.Value);
    }

    async Task NotifyClientsOfResults(int roomId, Guid winnerId, Guid loserId)
    {
        await _notifier.NotifyPlayerAboutOutcome(winnerId, roomId, true);
        await _notifier.NotifyPlayerAboutOutcome(loserId, roomId, false);
    }
}
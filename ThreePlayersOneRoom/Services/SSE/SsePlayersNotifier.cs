using System.Collections.Concurrent;
using System.Diagnostics;
using Lib.AspNetCore.ServerSentEvents;

namespace ThreePlayersOneRoom.Services.SSE;

public interface IPlayersNotifier
{
    Task NotifyPlayerAboutOutcome(Guid playerId, int roomId, bool hasPlayerWon);
}

public class SsePlayersNotifier :  IPlayersNotifier
{
    readonly ILogger<SsePlayersNotifier> _logger;
    readonly IServerSentEventsService _sse;

    // PlayerID, SseID
    internal readonly ConcurrentDictionary<Guid, Guid> ConnectedPlayers = new();

    readonly object _addingPlayerLock = new();

    public SsePlayersNotifier(ILogger<SsePlayersNotifier> logger, IServerSentEventsService sse)
    {
        _logger = logger;
        _sse = sse;
        _sse.ClientConnected += OnClientConnected;
        _sse.ClientDisconnected += OnClientDisconnected;
    }

    public async Task NotifyPlayerAboutOutcome(Guid playerId, int roomId, bool hasPlayerWon)
    {
        IServerSentEventsClient? client;

        // ReSharper disable once InconsistentlySynchronizedField
        // If we don't lock we may end up sending an event to a surpassed player's connection
        // which we don't really care about.
        if (!ConnectedPlayers.TryGetValue(playerId, out var clientId) ||
            (client = _sse.GetClient(clientId)) == null)
        {
            _logger.LogWarning("Cannot notify player {PlayerId} of an outcome because they are not connected.", playerId);
            return;
        }

        await client.SendEventAsync($"{roomId}: {hasPlayerWon}");
    }

    internal void OnClientConnected(object? sender, ServerSentEventsClientConnectedArgs e)
    {
        var playerId = GetPlayerId(e.Client);
        OnPlayerConnected(playerId, e.Client.Id);
    }

    internal void OnClientDisconnected(object? sender, ServerSentEventsClientDisconnectedArgs e)
    {
        var playerId = GetPlayerId(e.Client);
        OnPlayerDisconnected(playerId, e.Client.Id);
    }

    void OnPlayerConnected(Guid playerId, Guid sseId)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        // Here we don't care who's going to be removed because 
        // we're replacing them anyway.
        if (ConnectedPlayers.TryRemove(playerId, out var oldSseId))
        {
            // This user is already connected. Remove the old connection.
            var oldClient = _sse.GetClient(oldSseId);
            oldClient.Disconnect();
        }

        lock (_addingPlayerLock)
        {
            // We lock here to make sure that if another connection of 
            // this player is disconnecting we're waiting for it to remove itself
            // from the dictionary so that it doesn't accidentally remove us instead.
            ConnectedPlayers.TryAdd(playerId, sseId);
        }
    }

    void OnPlayerDisconnected(Guid playerId, Guid disconnectedSseId)
    {
        lock (_addingPlayerLock)
        {
            // Here we lock because we first need to check that no one has replaced us yet
            // and if not to remove ourselves from the dictionary.
            if (ConnectedPlayers.TryGetValue(playerId, out var sseId) &&
                disconnectedSseId == sseId)
            {
                // No one has replaced us. We're free to remove ourselves.
                ConnectedPlayers.TryRemove(playerId, out sseId);
                Debug.Assert(sseId == disconnectedSseId);
            }
        }
    }

    static Guid GetPlayerId(IServerSentEventsClient client) => Guid.Parse(client.User.Identity!.Name!);
}
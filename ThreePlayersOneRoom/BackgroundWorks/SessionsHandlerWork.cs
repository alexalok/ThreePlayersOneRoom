using System.Collections.Concurrent;
using ThreePlayersOneRoom.Handlers;

namespace ThreePlayersOneRoom.BackgroundWorks;

public class SessionsHandlerWork : BackgroundService
{
    readonly ILogger<SessionsHandlerWork> _logger;
    readonly IPendingSessionsProvider _pendingSessions;
    readonly ISessionHandler _sessionHandler;
    readonly ConcurrentDictionary<int, Task> _runningSessions = new();

    int _sessionId;

    public SessionsHandlerWork(ILogger<SessionsHandlerWork> logger, IPendingSessionsProvider pendingSessions, 
        ISessionHandler sessionHandler)
    {
        _logger = logger;
        _pendingSessions = pendingSessions;
        _sessionHandler = sessionHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        try
        {
            ExecuteInternal(stoppingToken);
        }
        catch (TaskCanceledException)
        {
            await Task.WhenAll(_runningSessions.Values);
        }
    }

    void ExecuteInternal(CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            
            _logger.LogTrace("Waiting for sessions...");

            int pendingSessionId = _pendingSessions.TakeRoomIdPendingSessionBlocking(ct);
            int sessionId = Interlocked.Decrement(ref _sessionId);
            _logger.LogDebug("Running new session: {SessionId}", sessionId);
            var t = _sessionHandler.HandleSession(pendingSessionId);
            _runningSessions.TryAdd(sessionId, t); // we won't fail here

            _ = t.ContinueWith(t =>
            {
                _runningSessions.TryRemove(sessionId, out _);
                _logger.LogTrace("Removed session {SessionId} from running sessions", sessionId);
            });
        }
    }
}

public interface ISessionRequester
{
    void RequestSessionForRoom(int roomId);
}

public interface IPendingSessionsProvider
{
    /// <exception cref="OperationCanceledException"></exception>
    int TakeRoomIdPendingSessionBlocking(CancellationToken ct);
}

public class SessionsStorage : ISessionRequester, IPendingSessionsProvider
{
    readonly BlockingCollection<int> _pendingSessionsIds = new();

    public void RequestSessionForRoom(int roomId)
    {
        _pendingSessionsIds.Add(roomId);
    }

    /// <inheritdoc />
    public int TakeRoomIdPendingSessionBlocking(CancellationToken ct)
    {
        // Will always return true due to an infinite timeout.
        // Will throw if ct is cancelled.
        _pendingSessionsIds.TryTake(out var pendingSessionId, Timeout.Infinite, ct);
        return pendingSessionId;
    }
}
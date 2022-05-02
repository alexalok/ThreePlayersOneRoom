using ThreePlayersOneRoom.Handlers;

namespace ThreePlayersOneRoom.Factories;

public class SessionHandlerFactory : ISessionHandler
{
    readonly IServiceProvider _services;

    public SessionHandlerFactory(IServiceProvider services)
    {
        _services = services;
    }

    public async Task HandleSession(int roomId)
    {
        await using var scope = _services.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<SessionHandler>();
        await runner.HandleSession(roomId);
    }
}
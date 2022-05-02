using ThreePlayersOneRoom.Repositories;

namespace ThreePlayersOneRoom.Factories;

public interface IRoomsRepositoryFactory
{
    IAsyncDisposable Create(out IRoomsRepository rooms);
}

public class RoomsRepositoryFactory : IRoomsRepositoryFactory
{
    readonly IServiceProvider _services;

    public RoomsRepositoryFactory(IServiceProvider services)
    {
        _services = services;
    }

    public IAsyncDisposable Create(out IRoomsRepository rooms)
    {
        var scope = _services.CreateAsyncScope();
        rooms = scope.ServiceProvider.GetRequiredService<IRoomsRepository>();
        return scope;
    }
}

using AspNetCore.Authentication.ApiKey;
using Lib.AspNetCore.ServerSentEvents;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ThreePlayersOneRoom.BackgroundWorks;
using ThreePlayersOneRoom.DB;
using ThreePlayersOneRoom.Factories;
using ThreePlayersOneRoom.Handlers;
using ThreePlayersOneRoom.Repositories;
using ThreePlayersOneRoom.Services.SSE;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var services = builder.Services;
var env = builder.Environment;

// Add services to the container.
services.AddControllers();
services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
    .AddApiKeyInAuthorizationHeader(opt =>
    {
        opt.KeyName = "bearer";
        opt.SuppressWWWAuthenticateHeader = true;
        opt.Events = new()
        {
            OnValidateKey = (ctx) =>
            {
                if (Guid.TryParse(ctx.ApiKey, out var guid))
                    ctx.ValidationSucceeded(guid.ToString());
                else
                    ctx.ValidationFailed("Invalid key format");
                return Task.CompletedTask;
            }
        };
    });

services.AddDbContext<GameDbContext>(opt =>
{
    opt.EnableSensitiveDataLogging(env.IsDevelopment());
    SqliteConnectionStringBuilder connStringBuilder = new()
    {
        DataSource = Path.Combine(env.ContentRootPath, config["DbName"])
    };
    opt.UseSqlite(connStringBuilder.ConnectionString);
});

services.AddServerSentEvents()
    .AddServerSentEventsClientIdProvider<CookieBasedServerSentEventsClientIdProvider>()
    .AddInMemoryServerSentEventsNoReconnectClientsIdsStore();
services.AddSingleton<IPlayersNotifier, SsePlayersNotifier>();

services.AddTransient<IRoomsRepository, RoomsRepository>();
services.AddSingleton<IRoomsRepositoryFactory, RoomsRepositoryFactory>();

services.AddSingleton<ISessionHandler, SessionHandlerFactory>();
services.AddTransient<SessionHandler>();
services.AddHostedService<SessionsHandlerWork>();

services.AddSingleton<SessionsStorage>();
services.AddSingleton<IPendingSessionsProvider>(s => s.GetRequiredService<SessionsStorage>());
services.AddSingleton<ISessionRequester>(s => s.GetRequiredService<SessionsStorage>());

var app = builder.Build();

// We have to explicitly create it so that it subscribes to SSE service in .ctor
// Otherwise it'll be created by SessionRunner but it will skip players who subscribed before
// a first ever session runs.
_ = app.Services.GetRequiredService<IPlayersNotifier>();

// Configure the HTTP request pipeline.

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.MapServerSentEvents("/sse", new()
{
    Authorization = ServerSentEventsAuthorization.Default
});

app.Run();
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Lib.AspNetCore.ServerSentEvents;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ThreePlayersOneRoom.Services.SSE;
using Xunit;

namespace ThreePlayersOneRoom.Tests;

public class SsePlayersNotifier_Tests
{
    static readonly Guid PlayerId = Guid.Parse("f112384c-cbac-4f9e-b478-fc9e5cd10db3");

    [Fact]
    public async Task Ensure_Duplicate_Player_Connection_Gets_Replaced()
    {
        // Arrange
        var conn1Guid = Guid.NewGuid();
        var conn2Guid = Guid.NewGuid();

        ClaimsPrincipal principal = new(new ClaimsIdentity(new Claim[]
        {
            new(ClaimTypes.Name, PlayerId.ToString())
        }));

        Mock<IServerSentEventsClient> client1 = new(MockBehavior.Strict);
        client1.Setup(c => c.Id)
            .Returns(conn1Guid);
        client1.Setup(c => c.User)
            .Returns(principal);
        client1.Setup(c => c.Disconnect());

        Mock<IServerSentEventsClient> client2 = new(MockBehavior.Strict);
        client2.Setup(c => c.Id)
            .Returns(conn2Guid);
        client2.Setup(c => c.User)
            .Returns(principal);
        client2.Setup(c => c.Disconnect());

        Mock<IServerSentEventsService> sse = new(MockBehavior.Strict);
        sse.Setup(s => s.GetClient(conn1Guid))
            .Returns(client1.Object);
        sse.Setup(s => s.GetClient(conn2Guid))
            .Returns(client2.Object);

        SsePlayersNotifier notifier = new(NullLogger<SsePlayersNotifier>.Instance, sse.Object);

        // Act
        notifier.OnClientConnected(null, new(null, client1.Object));
        notifier.OnClientConnected(null, new(null, client2.Object));

        // Assert
        Assert.Single(notifier.ConnectedPlayers);
        Assert.Equal(conn2Guid, notifier.ConnectedPlayers.Single().Value);
    }
}
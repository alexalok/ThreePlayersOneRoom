using System;
using ThreePlayersOneRoom.Models;
using Xunit;

namespace ThreePlayersOneRoom.Tests;

public class Session_Tests
{
    static readonly Guid Player1Id = Guid.Parse("f112384c-cbac-4f9e-b478-fc9e5cd10db3");
    static readonly Guid Player2Id = Guid.Parse("cbe81f5e-442b-4811-8eeb-a33790e3fbab");

    [Fact]
    public void Ensure_WinnerId_And_LoserId_Set_Properly_When_Player1_Wins()
    {
        // Arrange
        Session session = new(Player1Id, Player2Id);

        // Act
        session.Advance(0, Session.DefaultPlayersHealth);

        // Assert
        Assert.Equal(Player1Id, session.WinnerId);
        Assert.Equal(Player2Id, session.LoserId);
    }

    [Fact]
    public void Ensure_WinnerId_And_LoserId_Set_Properly_When_Player2_Wins()
    {
        // Arrange
        Session session = new(Player1Id, Player2Id);

        // Act
        session.Advance(Session.DefaultPlayersHealth, 0);

        // Assert
        Assert.Equal(Player2Id, session.WinnerId);
        Assert.Equal(Player1Id, session.LoserId);
    }

    [Fact]
    public void Ensure_CanAdvance_Sets_To_False_Upon_Winning()
    {
        // Arrange
        Session session = new(Player1Id, Player2Id);

        // Act
        session.Advance(Session.DefaultPlayersHealth, 0);

        // Assert
        Assert.False(session.CanAdvance);
    }
}
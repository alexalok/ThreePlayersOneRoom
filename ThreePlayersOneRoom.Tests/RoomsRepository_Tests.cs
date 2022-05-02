using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ThreePlayersOneRoom.DB;
using ThreePlayersOneRoom.DB.Entities;
using ThreePlayersOneRoom.Exceptions;
using ThreePlayersOneRoom.Repositories;
using Xunit;

namespace ThreePlayersOneRoom.Tests;

public class RoomsRepository_Tests : IDisposable
{
    static readonly Guid HostId = Guid.Parse("f112384c-cbac-4f9e-b478-fc9e5cd10db3");
    static readonly Guid FollowerId = Guid.Parse("cbe81f5e-442b-4811-8eeb-a33790e3fbab");

    readonly string _dbFilePath;
    readonly GameDbContext _db;

    public RoomsRepository_Tests()
    {
        _dbFilePath = Path.GetTempFileName();

        // IMPORTANT: we have to disable pooling here, otherwise Dispose() will 
        // crash when trying to delete a db file.
        _db = new(new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlite($"Data Source={_dbFilePath};Pooling=false;").Options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Ensure_JoinRoom_Throws_TryingToJoinOwnRoomException_If_User_Joins_Own_Room()
    {
        // Arrange
        Room room = new()
        {
            Id = 1,
            HostId = HostId,
        };
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        RoomsRepository rooms = new(_db);

        // Act 
        var action = () => rooms.JoinRoom(room.Id, HostId);

        // Assert
        await Assert.ThrowsAsync<TryingToJoinOwnRoomException>(action);
    }
    
    [Fact]
    public async Task Ensure_JoinRoom_Throws_RoomIsFullException_If_User_Joins_Room_That_Already_Has_A_Follower()
    {
        // Arrange
        Room room = new()
        {
            Id = 1,
            HostId = HostId,
            FollowerId = FollowerId
        };
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        RoomsRepository rooms = new(_db);

        // Act 
        var action = () => rooms.JoinRoom(room.Id, FollowerId);

        // Assert
        await Assert.ThrowsAsync<RoomIsFullException>(action);
    }

    /// <summary>
    /// During JoinRoom() there is a small window when a race condition between two players
    /// joining the same room might occur. Make sure that the slowest player gets an exception instead of
    /// overwriting the player that has joined first.
    /// </summary>
    [Fact]
    public async Task Ensure_JoinRoom_Throws_RoomIsFullException_If_User_Tries_To_Join_When_Another_User_Already_Joins()
    {
        // Arrange
        Room room = new()
        {
            Id = 1,
            HostId = HostId,
        };
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        RoomsRepository rooms = new(_db);

        // Act 
        var t1 =  rooms.JoinRoom(room.Id, FollowerId);
        var t2 = rooms.JoinRoom(room.Id, FollowerId);

        // Assert
        await Assert.ThrowsAsync<RoomIsFullException>(() => t2);
        await t1;
    }

    [Theory]
    [MemberData(nameof(Ensure_SetWinner_Sets_Outcome_Correctly_Data))]
    public async Task Ensure_SetWinner_Sets_Outcome_Correctly(Guid winnerGuid, Outcome expectedOutcome)
    {
        // Arrange
        Room room = new()
        {
            Id = 1,
            HostId = HostId,
            FollowerId = FollowerId
        };
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        RoomsRepository rooms = new(_db);

        // Act
        await rooms.SetWinner(room.Id, winnerGuid);

        // Assert
        Assert.Equal(expectedOutcome, room.Outcome);
    }

    public void Dispose()
    {
        File.Delete(_dbFilePath);
    }

    public static IEnumerable<object[]> Ensure_SetWinner_Sets_Outcome_Correctly_Data => new[]
    {
        new object[] {HostId, Outcome.HostWon},
        new object[] {FollowerId, Outcome.FollowerWon}
    };
}
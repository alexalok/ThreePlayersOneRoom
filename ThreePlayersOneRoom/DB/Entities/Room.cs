using System.ComponentModel.DataAnnotations;

namespace ThreePlayersOneRoom.DB.Entities;

public class Room
{
    public int Id { get; set; }
    public Guid HostId { get; set; } 

    [ConcurrencyCheck]
    public Guid? FollowerId { get; set; }

    public Outcome? Outcome { get; set; }
}

public enum Outcome
{
    HostWon = 1,
    FollowerWon
}
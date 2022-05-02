using Microsoft.EntityFrameworkCore;
using ThreePlayersOneRoom.DB.Entities;

namespace ThreePlayersOneRoom.DB;

public class GameDbContext : DbContext
{
    public DbSet<Room> Rooms { get; set; } = null!;

    public GameDbContext(DbContextOptions opt) : base(opt)
    {
    }
}
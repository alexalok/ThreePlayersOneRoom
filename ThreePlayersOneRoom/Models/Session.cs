using System.Diagnostics.CodeAnalysis;

namespace ThreePlayersOneRoom.Models;

public class Session
{
    internal const int DefaultPlayersHealth = 10;

    [MemberNotNullWhen(false, nameof(WinnerId))]
    [MemberNotNullWhen(false, nameof(LoserId))]
    public bool CanAdvance { get; private set; } = true;

    public Guid? WinnerId { get; private set; }
    public Guid? LoserId { get; private set; }

    readonly Player _player1;
    readonly Player _player2;

    public Session(Guid player1Id, Guid player2Id)
    {
        _player1 = new(player1Id, DefaultPlayersHealth);
        _player2 = new(player2Id, DefaultPlayersHealth);
    }

    public void Advance(int damageToPlayer1, int damageToPlayer2)
    {
        if (WinnerId != null)
            throw new InvalidOperationException("Session has already ended.");

        _player1.TakeDamage(damageToPlayer1);
        if (!_player1.IsAlive)
        {
            WinnerId = _player2.Id;
            LoserId = _player1.Id;
            CanAdvance = false;
        }

        _player2.TakeDamage(damageToPlayer2);
        if (!_player2.IsAlive)
        {
            WinnerId = _player1.Id;
            LoserId = _player2.Id;
            CanAdvance = false;
        }
    }

    class Player
    {
        public Guid Id { get; }
        public int Health { get; private set; }

        public bool IsAlive => Health > 0;

        public Player(Guid id, int health)
        {
            Id = id;
            Health = health;
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
        }
    }
}
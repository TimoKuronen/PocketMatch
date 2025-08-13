using System;
using UnityEngine;

public class DestroyableTileData : TileData, IDamageableTile
{
    public int HitPoints { get; private set; }
    public bool CanMove { get; }
    public bool IsDestroyed => HitPoints <= 0;

    public event Action<int> OnTakeDamage;

    public DestroyableTileData(Vector2Int pos, int initialHP, bool canMove) : base(TileType.Red, pos)
    {
        HitPoints = initialHP;
        CanMove = canMove;
        State = TileState.Destroyable;
    }

    public void TakeDamage(int amount)
    {
        HitPoints -= amount;
        OnTakeDamage?.Invoke(HitPoints);

        if (HitPoints <= 0)
        {
            State = TileState.Empty;
            Power = TilePower.None;
        }
    }
}
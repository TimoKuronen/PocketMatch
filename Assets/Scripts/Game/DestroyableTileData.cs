using System;
using UnityEngine;

public class DestroyableTileData : TileData, IDamageableTile
{
    public int HitPoints { get; private set; }
    public bool IsDestroyed => HitPoints <= 0;

    public event Action<int> OnTakeDamage;

    public DestroyableTileData(Vector2Int pos, int initialHP) : base(TileType.Red, pos)
    {
        HitPoints = initialHP;
        State = TileState.Destroyable;
    }

    public void TakeDamage(int amount)
    {
        HitPoints -= amount;
        OnTakeDamage?.Invoke(HitPoints);
    }
}
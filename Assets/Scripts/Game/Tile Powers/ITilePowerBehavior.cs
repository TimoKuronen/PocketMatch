using UnityEngine;

public interface ITilePowerBehavior
{
    void Apply(Vector2Int origin, GridContext context);
}

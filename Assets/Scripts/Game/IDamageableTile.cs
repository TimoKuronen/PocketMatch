public interface IDamageableTile
{
    int HitPoints { get; }
    bool IsDestroyed { get; }
    void TakeDamage(int amount);
}
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage);
    void TakeKnockback(Vector2 direction, float force);
    bool IsDead();
}
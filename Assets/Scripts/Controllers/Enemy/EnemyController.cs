using Fusion;
using UnityEngine;

public class EnemyController : NetworkBehaviour, IDamageable
{
    [Networked] public int Health { get; set; }
    [Networked] public NetworkBool isDeadNetworked { get; set; }

    public int maxHealth = 3;
    public Animator anim;
    private Rigidbody2D rb;

    public override void Spawned()
    {
        if (HasStateAuthority) Health = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int damage)
    {
        if (!HasStateAuthority || isDeadNetworked) return;

        Health -= damage;
        if (Health <= 0) Die();
        else anim.SetTrigger("Hit");
    }

    public void TakeKnockback(Vector2 direction, float force)
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(direction.x * force, rb.linearVelocity.y);
    }

   
    public bool IsDead => isDeadNetworked;

    private void Die()
    {
        isDeadNetworked = true;
        if (anim != null) anim.SetBool("IsDead", true);

        
        Runner.Despawn(Object);
    }
}
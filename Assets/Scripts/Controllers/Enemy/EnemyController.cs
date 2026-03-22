using Fusion;
using UnityEngine;

public class EnemyController : NetworkBehaviour, IDamageable
{
    [Header("---- COMPONENTS ----")]
    public Animator anim;
    private Rigidbody2D rb;
    private Collider2D myCollider;
    private EnemyAI enemyAI;

    [Header("---- STATS ----")]
    [Networked] public int Health { get; set; }
    [Networked] public NetworkBool isDeadNetworked { get; set; }
    public int maxHealth = 3;
    public int attackDamage = 1;
    public float knockbackForce = 8f;

    [Header("---- ATTACK RANGE & ANGLE ----")]
    public Transform attackPoint;
    public float attackRange = 1.5f;
    [Range(0, 360)] public float attackAngle = 90f;
    public LayerMask playerLayer;

    [Header("---- SETTINGS ----")]
    public float attackRecoveryTime = 1.0f;
    public float deathAnimDuration = 1.133f;
    public float knockbackDuration = 0.2f;

    [Header("---- DEATH PHYSICS ----")]
    public Vector2 deadColliderSize = new Vector2(1f, 0.2f);
    public Vector2 deadColliderOffset = new Vector2(0f, -0.5f);

    [Networked] public NetworkBool IsAttacking { get; set; }
    [Networked] public NetworkBool IsKnockbackActive { get; set; }

    [Networked] private TickTimer attackTimer { get; set; }
    [Networked] private TickTimer knockbackTimer { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority) Health = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        enemyAI = GetComponent<EnemyAI>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || isDeadNetworked) return;

        if (IsAttacking && attackTimer.Expired(Runner))
            IsAttacking = false;

        if (IsKnockbackActive && knockbackTimer.Expired(Runner))
            IsKnockbackActive = false;
    }

    public void AttemptAttack()
    {
        if (isDeadNetworked || IsAttacking) return;

        if (attackTimer.ExpiredOrNotRunning(Runner))
        {
            IsAttacking = true;

            // Random 2 đòn đánh (Index 0 hoặc 1)
            int randomAttackIndex = Random.Range(0, 2);

            // Bắn RPC để tất cả Client thấy đòn đánh ngẫu nhiên này
            RPC_PlayAttackAnim(randomAttackIndex);

            // Cài đặt thời gian hồi đòn
            attackTimer = TickTimer.CreateFromSeconds(Runner, attackRecoveryTime + 0.5f);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAttackAnim(int attackIndex)
    {
        if (anim != null)
        {
            anim.SetInteger("AttackIndex", attackIndex); // Nhớ tạo biến Int "AttackIndex" trong Animator
            anim.SetTrigger("Attack");
        }
    }

    // Gắn hàm này vào Animation Event ở frame chém
    public void TriggerAttackDamage()
    {
        // Máy dò 1:
        Debug.Log(">>> [QUÁI] Đã bổ rìu!");

        if (!HasStateAuthority || isDeadNetworked) return;
        if (attackPoint == null) { Debug.LogError("[QUÁI] Chưa gắn Attack Point!"); return; }

        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);

        // Máy dò 2:
        Debug.Log($"[QUÁI] Vòng tròn quét trúng {hitPlayers.Length} vật thể thuộc layer {LayerMask.LayerToName(playerLayer)}.");

        foreach (var player in hitPlayers)
        {
            Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
            Vector2 facingDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

            if (Vector2.Angle(facingDirection, directionToPlayer) < attackAngle / 2f)
            {
                IDamageable dmg = player.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    Debug.Log($"[QUÁI] Đã chém trúng người chơi: {player.name}");
                    dmg.TakeDamage(attackDamage);
                    Vector2 pushDir = new Vector2(directionToPlayer.x, 0.5f).normalized;
                    dmg.TakeKnockback(pushDir, knockbackForce);
                }
                else
                {
                    Debug.LogWarning($"[QUÁI] Quét trúng {player.name} nhưng nó KHÔNG có script chứa IDamageable!");
                }
            }
            else
            {
                Debug.Log($"[QUÁI] Quét trúng {player.name} nhưng nó đứng ngoài góc chém {attackAngle} độ!");
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDeadNetworked) return;
        RPC_TakeDamage(damage);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_TakeDamage(int damage)
    {
        if (isDeadNetworked) return;

        Health -= damage;
        if (enemyAI != null) enemyAI.ForceFacePlayer(); // Quay mặt lại đánh

        if (Health <= 0) Die();
        else
        {
            if (anim != null && !IsAttacking) anim.SetTrigger("Hit"); // Super Armor: Đang chém thì không giật
        }
    }

    public void TakeKnockback(Vector2 direction, float force)
    {
        RPC_TakeKnockback(direction, force);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_TakeKnockback(Vector2 direction, float force)
    {
        if (isDeadNetworked) return;

        IsKnockbackActive = true;
        knockbackTimer = TickTimer.CreateFromSeconds(Runner, knockbackDuration);

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }

    public bool IsDead => isDeadNetworked;

    private void Die()
    {
        isDeadNetworked = true;
        IsAttacking = false;
        IsKnockbackActive = false;
        rb.linearVelocity = Vector2.zero;

        if (anim != null) anim.SetBool("IsDead", true);

        // Thu nhỏ Collider khi chết
        if (myCollider is CapsuleCollider2D capsule)
        {
            capsule.direction = CapsuleDirection2D.Horizontal;
            capsule.size = deadColliderSize;
            capsule.offset = deadColliderOffset;
        }
        else if (myCollider is BoxCollider2D box)
        {
            box.size = deadColliderSize;
            box.offset = deadColliderOffset;
        }

        Invoke(nameof(DespawnEnemy), deathAnimDuration);
    }

    private void DespawnEnemy() { if (HasStateAuthority) Runner.Despawn(Object); }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        Vector3 facingDirection = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
        Vector3 upperLimit = Quaternion.Euler(0, 0, attackAngle / 2f) * facingDirection;
        Vector3 lowerLimit = Quaternion.Euler(0, 0, -attackAngle / 2f) * facingDirection;

        Gizmos.color = new Color(1, 0.92f, 0.016f, 0.7f);
        Gizmos.DrawRay(transform.position, upperLimit * attackRange);
        Gizmos.DrawRay(transform.position, lowerLimit * attackRange);
    }
}
using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("---- COMPONENTS ----")]
    public Animator anim;
    private Rigidbody2D rb;
    private Collider2D myCollider;
    private EnemyAI enemyAI;

    [Header("---- ANIMATION DATA ----")]
    public AnimationClip[] attackClips;

    [Header("---- STATS ----")]
    public int maxHealth = 3;
    public int attackDamage = 1;
    public float knockbackForce = 8f;

    [Header("---- SETTINGS ----")]
    public float attackRecoveryTime = 1.0f;
    public float deathAnimDuration = 1.133f;
    public float knockbackDuration = 0.2f;

    [Header("---- DEATH PHYSICS ----")]
    public Vector2 deadColliderSize = new Vector2(1f, 0.2f);
    public Vector2 deadColliderOffset = new Vector2(0f, -0.5f);

    [Header("---- ATTACK RANGE ----")]
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public LayerMask playerLayer;

    [Range(0, 360)]
    public float attackAngle = 90f;

    private int currentHealth;
    private float nextAttackTime = 0f;

    private bool isDead = false;

    public bool IsHurting { get; private set; }
    public bool IsAttacking { get; private set; }
    public bool IsKnockbackActive { get; private set; }

    void Start()
    {
        currentHealth = maxHealth;
        if (anim == null) anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        enemyAI = GetComponent<EnemyAI>();
    }

    public void AttemptAttack()
    {
        if (isDead || IsHurting || IsAttacking) return;

        if (Time.time >= nextAttackTime)
        {
            IsAttacking = true;
            if (anim != null && attackClips.Length > 0)
            {
                int index = Random.Range(0, attackClips.Length);
                float animDuration = attackClips[index].length;
                anim.SetInteger("AttackIndex", index);
                anim.SetTrigger("Attack");

                float totalWaitTime = animDuration + attackRecoveryTime;
                Invoke("FinishAttack", totalWaitTime);
                nextAttackTime = Time.time + totalWaitTime;
            }
            else { IsAttacking = false; }
        }
    }

    void FinishAttack() { IsAttacking = false; }

    public void TriggerAttackDamage()
    {
        if (isDead) return;

        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);

        foreach (Collider2D target in hitTargets)
        {
            Vector2 directionToTarget = (target.transform.position - transform.position).normalized;
            Vector2 facingDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

            if (Vector2.Angle(facingDirection, directionToTarget) < attackAngle / 2f)
            {
                IDamageable damageableTarget = target.GetComponent<IDamageable>();

                if (damageableTarget != null)
                {
                    damageableTarget.TakeDamage(attackDamage);

                    Vector2 pushDir = (target.transform.position - transform.position).normalized;
                    pushDir = new Vector2(pushDir.x, 0.5f).normalized;

                    damageableTarget.TakeKnockback(pushDir, knockbackForce);
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (enemyAI != null) enemyAI.ForceFacePlayer();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (anim != null && !IsAttacking)
            {
                anim.SetTrigger("Hit");
            }
        }
    }

    // --- ĐÃ SỬA LẠI HÀM NÀY ĐỂ BẤT CHẤP KHỐI LƯỢNG (MASS) ---
    public void TakeKnockback(Vector2 direction, float force)
    {
        if (isDead) return;

        IsKnockbackActive = true;

        // Cố tình bỏ qua AddForce, thiết lập thẳng vận tốc cho quái vật
        // Nhân thêm với Mass để lực đẩy luôn chuẩn xác dù quái nặng 1kg hay 500kg
        rb.linearVelocity = new Vector2(direction.x * force, rb.linearVelocity.y);

        CancelInvoke("StopKnockbackPhysic");
        Invoke("StopKnockbackPhysic", knockbackDuration);
    }

    void StopKnockbackPhysic()
    {
        IsKnockbackActive = false;
        // Trả lại vận tốc X về 0 sau khi hết bị đẩy lùi
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        IsHurting = true;
        IsAttacking = false;
        IsKnockbackActive = false;

        if (anim != null) anim.SetBool("IsDead", true);

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (myCollider != null)
        {
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

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject p in players)
            {
                Collider2D playerCollider = p.GetComponent<Collider2D>();
                if (playerCollider != null) Physics2D.IgnoreCollision(myCollider, playerCollider, true);
            }
        }

        Destroy(gameObject, deathAnimDuration + 0.5f);
    }

    public bool IsDead() => isDead;

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
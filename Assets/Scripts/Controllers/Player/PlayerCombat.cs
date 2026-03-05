using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("---- REFERENCES ----")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator anim; // Đã thêm Animator vào đây để gom về 1 mối

    [Header("---- ATTACK SETTINGS ----")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask targetLayers;

    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float knockbackForceToEnemy = 8f;
    public float attackRate = 2f;

    [Range(0, 360)]
    [SerializeField] private float attackAngle = 90f;

    private float nextAttackTime = 0f;

    // Cờ khóa chém để chống spam
    public bool isAttacking { get; private set; }

    void Start()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (anim == null) anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (!playerController.isLocalPlayer || playerController.IsDead()) return;

        // Cơ chế an toàn dự phòng: tự mở khóa chém nếu bị kẹt do dính đòn (Hit)
        if (isAttacking && Time.time >= nextAttackTime)
        {
            isAttacking = false;
        }

        // Chống spam: Nếu ĐANG CHÉM thì tuyệt đối không nhận lệnh chém nữa
        if (Time.time >= nextAttackTime && !isAttacking)
        {
            if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
            {
                isAttacking = true;
                anim.SetTrigger("Attack"); // Chỉ phát hình ảnh, KHÔNG gây sát thương ngay

                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
    }

    // --- HÀM EVENT 1: Sẽ được gọi ngay khung hình mà thanh kiếm chém xuống ---
    public void TriggerAttackDamage()
    {
        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, targetLayers);

        foreach (Collider2D targetCollider in hitTargets)
        {
            Vector2 directionToTarget = (targetCollider.transform.position - transform.position).normalized;
            Vector2 facingDirection = playerController.IsFacingRight ? Vector2.right : Vector2.left;

            float angleToTarget = Vector2.Angle(facingDirection, directionToTarget);

            if (angleToTarget < attackAngle / 2f)
            {
                IDamageable damageableTarget = targetCollider.GetComponent<IDamageable>();

                if (damageableTarget != null)
                {
                    damageableTarget.TakeDamage(attackDamage);

                    Vector2 pushDir = (targetCollider.transform.position - transform.position).normalized;
                    pushDir = new Vector2(pushDir.x, 0.2f).normalized;

                    damageableTarget.TakeKnockback(pushDir, knockbackForceToEnemy);
                }
            }
        }
    }

    // --- HÀM EVENT 2: Sẽ được gọi ở khung hình kết thúc hoạt ảnh chém ---
    public void FinishAttack()
    {
        isAttacking = false;
        // Quét sạch mọi lệnh bấm J bị dư (buffer) do spam quá nhanh
        anim.ResetTrigger("Attack");
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        Vector3 facingDirection = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
        Vector3 upperLimit = Quaternion.Euler(0, 0, attackAngle / 2f) * facingDirection;
        Vector3 lowerLimit = Quaternion.Euler(0, 0, -attackAngle / 2f) * facingDirection;

        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawRay(attackPoint.position, upperLimit * attackRange);
        Gizmos.DrawRay(attackPoint.position, lowerLimit * attackRange);
    }
}
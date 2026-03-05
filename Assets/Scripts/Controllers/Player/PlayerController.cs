using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("---- NETWORK CONFIG ----")]
    [Tooltip("Đánh dấu true nếu nhân vật này do máy hiện tại điều khiển. NetworkSpawner sẽ tự động gán giá trị này khi sinh nhân vật ra.")]
    public bool isLocalPlayer = true;

    [Header("---- MOVEMENT SETTINGS ----")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 15f;

    [Header("---- PHYSICS SETTINGS ----")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("---- HEALTH & STATS ----")]
    public int maxHP = 100;
    public int currentHP;
    public float invincibleTime = 0.8f;

    private float horizontalInput;
    public bool isGrounded;
    private bool isKnockedBack = false;
    private bool isInvincible = false;
    private bool isDead = false;

    private SpriteRenderer sr;
    private int playerLayer;
    private int enemyLayer;
    private int bossLayer;

    public bool IsFacingRight { get; private set; } = true;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        currentHP = maxHP;
        playerLayer = gameObject.layer;

        // [ĐÃ SỬA]: Lấy đúng ID của cả Boss và Enemy
        bossLayer = LayerMask.NameToLayer("Boss");
        enemyLayer = LayerMask.NameToLayer("Enemy");

        // Nếu đây là nhân vật của người chơi khác qua mạng (remote player)
        // Tắt mô phỏng vật lý đi để nhận vị trí trực tiếp từ Server/Host gửi về
        if (!isLocalPlayer)
        {
            rb.isKinematic = true;
        }
    }

    void Update()
    {
        if (!isLocalPlayer || isKnockedBack || isDead) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");

        CheckGround();
        HandleJump();
        HandleFlip();
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer || isKnockedBack || isDead) return;

        Move();
    }

    void CheckGround()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.W) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void Move()
    {
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    void HandleFlip()
    {
        if (horizontalInput > 0 && !IsFacingRight) Flip();
        else if (horizontalInput < 0 && IsFacingRight) Flip();
    }

    void Flip()
    {
        IsFacingRight = !IsFacingRight;
        Vector3 currentScale = transform.localScale;
        transform.localScale = new Vector3(currentScale.x * -1, currentScale.y, currentScale.z);
    }

    // --- IMPLEMENT IDAMAGEABLE ---
    public void TakeDamage(int damage)
    {
        if (isInvincible || isDead) return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0);
        Debug.Log("Player HP: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibleCoroutine());
        }
    }

    // Hàm này sau này HauLT sẽ gọi thông qua gói tin mạng nếu người chơi kia bị đẩy lùi
    public void TakeKnockback(Vector2 direction, float force)
    {
        if (isDead) return;

        isKnockedBack = true;

        if (isLocalPlayer)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }

        Invoke("StopKnockback", 0.2f);
    }

    public bool IsDead() => isDead;

    void StopKnockback() => isKnockedBack = false;

    // --- LOGIC BẤT TỬ & CHẾT ---
    IEnumerator InvincibleCoroutine()
    {
        isInvincible = true;

        // [ĐÃ SỬA]: Kiểm tra an toàn trước khi bỏ qua va chạm với CẢ Boss VÀ Enemy
        if (bossLayer != -1) Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, true);
        if (enemyLayer != -1) Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        float timer = 0f;
        while (timer < invincibleTime)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        if (sr != null) sr.enabled = true;

        // [ĐÃ SỬA]: Bật lại va chạm sau khi hết thời gian bất tử
        if (bossLayer != -1) Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, false);
        if (enemyLayer != -1) Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);

        isInvincible = false;
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        Debug.Log("Player Dead");
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
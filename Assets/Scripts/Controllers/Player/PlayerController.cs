using Fusion;
using UnityEngine;
using System.Collections;

public class PlayerController : NetworkBehaviour, IDamageable
{
    [Header("---- MOVEMENT ----")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float jumpCutMultiplier = 0.4f;

    [Header("---- PHYSICS ----")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.05f; // Thay thế cho Radius cũ

    [Networked] public int currentHP { get; set; }
    [Networked] public NetworkBool isDeadNetworked { get; set; }
    [Networked] public NetworkBool IsGrounded { get; set; }
    [Networked] public NetworkBool IsFacingRight { get; set; } = true;

    [Networked] private NetworkButtons _buttonsPrev { get; set; }

    public int maxHP = 100;
    private bool isInvincible = false;
    private SpriteRenderer sr;

    public override void Spawned()
    {
        if (HasStateAuthority) currentHP = maxHP;
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    public override void FixedUpdateNetwork()
    {
        if (isDeadNetworked) return;

        // Gọi hàm quét đất bằng công nghệ chuẩn Mạng!
        CheckGround();

        if (GetInput(out NetworkInputData data))
        {
            rb.linearVelocity = new Vector2(data.horizontalInput * moveSpeed, rb.linearVelocity.y);

            var pressed = data.buttons.GetPressed(_buttonsPrev);
            var released = data.buttons.GetReleased(_buttonsPrev);

            if (pressed.IsSet(MyButtons.Jump) && IsGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            if (pressed.IsSet(MyButtons.Jump) && IsGrounded)
            {
                // MẸO SỬA LỖI: Nhấc nhẹ vị trí Rigidbody lên 0.05f 
                // để thoát khỏi sự lún vật lý (Depenetration) trước khi cấp lực
                rb.position = new Vector2(rb.position.x, rb.position.y + 0.05f);

                // Sau đó mới set lại vận tốc nhảy
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            if (data.horizontalInput > 0) IsFacingRight = true;
            else if (data.horizontalInput < 0) IsFacingRight = false;

            _buttonsPrev = data.buttons;
        }
    }

    public override void Render()
    {
        float targetScale = IsFacingRight ? Mathf.Abs(transform.localScale.x) : -Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(targetScale, transform.localScale.y, transform.localScale.z);
    }

    // NÂNG CẤP HÀM CHECK GROUND DÀNH RIÊNG CHO PHOTON FUSION
    void CheckGround()
    {
        // 1. Khai báo bộ lọc chỉ quét các vật thể thuộc Layer "Ground"
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);

        RaycastHit2D[] hits = new RaycastHit2D[1];

        // 2. Tùyệt chiêu rb.Cast(): 
        // Lấy chính cái Capsule Collider của nhân vật ấn xuống dưới 0.05m. 
        // Cách này ép Fusion Physics phải trực tiếp trả lời xem nó có đè lên đất không!
        int count = rb.Cast(Vector2.down, filter, hits, groundCheckDistance);

        IsGrounded = count > 0;
    }

    public bool IsDead => isDeadNetworked;

    public void TakeDamage(int damage)
    {
        if (isInvincible || isDeadNetworked || !HasStateAuthority) return;
        currentHP -= damage;
        if (currentHP <= 0) Die();
        else StartCoroutine(InvincibleCoroutine());
    }

    public void TakeKnockback(Vector2 direction, float force)
    {
        if (isDeadNetworked) return;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }

    private void Die()
    {
        isDeadNetworked = true;
        rb.linearVelocity = Vector2.zero;
    }

    IEnumerator InvincibleCoroutine()
    {
        isInvincible = true;
        float timer = 0.8f;
        while (timer > 0)
        {
            if (sr) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.1f);
            timer -= 0.1f;
        }
        if (sr) sr.enabled = true;
        isInvincible = false;
    }
}
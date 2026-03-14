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
    [SerializeField] private float groundCheckDistance = 0.05f;

    [Networked] public int currentHP { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public NetworkBool isDeadNetworked { get; set; }
    [Networked] public NetworkBool IsGrounded { get; set; }
    [Networked] public NetworkBool IsFacingRight { get; set; } = true;

    // BIẾN MẠNG ĐỂ TỐI ƯU ANIMATION CLIENT
    [Networked] public NetworkBool IsMoving { get; set; }
    [Networked] public float NetworkVelocityY { get; set; }

    [Networked] private NetworkButtons _buttonsPrev { get; set; }

    public int maxHP = 100;
    private bool isInvincible = false;
    private SpriteRenderer sr;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            currentHP = maxHP;
            PlayerName = $"Player {Object.InputAuthority.PlayerId}";
        }
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    public override void FixedUpdateNetwork()
    {
        if (isDeadNetworked) return;

        CheckGround();

        if (GetInput(out NetworkInputData data))
        {
            rb.linearVelocity = new Vector2(data.horizontalInput * moveSpeed, rb.linearVelocity.y);

            // Cập nhật trạng thái trực tiếp vào mạng
            IsMoving = Mathf.Abs(data.horizontalInput) > 0.1f;
            NetworkVelocityY = rb.linearVelocity.y;

            var pressed = data.buttons.GetPressed(_buttonsPrev);
            var released = data.buttons.GetReleased(_buttonsPrev);

            // Logic nhảy chuẩn (Đã dọn dẹp code thừa và chống lún vật lý)
            if (pressed.IsSet(MyButtons.Jump) && IsGrounded)
            {
                rb.position = new Vector2(rb.position.x, rb.position.y + 0.05f);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            if (released.IsSet(MyButtons.Jump) && rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }

            if (data.horizontalInput > 0) IsFacingRight = true;
            else if (data.horizontalInput < 0) IsFacingRight = false;

            _buttonsPrev = data.buttons;
        }
    }

    public override void Render()
    {
        if (sr != null)
        {
            // Tối ưu siêu việt: Lật ảnh bằng Card đồ họa, không lật Transform
            sr.flipX = !IsFacingRight;
        }
    }

    void CheckGround()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        RaycastHit2D[] hits = new RaycastHit2D[1];
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
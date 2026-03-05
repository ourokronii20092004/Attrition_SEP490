using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("---- COMPONENTS ----")]
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody2D rb;

    private int lastHP;

    void Start()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (playerController != null) lastHP = playerController.maxHP;
    }

    void Update()
    {
        HandleAnimations();
        // ĐÃ XÓA hàm HandleCombatInput() ở đây để chống lỗi chém dư.
    }

    private void HandleAnimations()
    {
        if (playerController == null) return;

        if (playerController.IsDead())
        {
            anim.SetBool("IsDead", true);
            return;
        }

        if (playerController.currentHP < lastHP)
        {
            anim.SetTrigger("Hit");
            lastHP = playerController.currentHP;
        }
        else if (playerController.currentHP > lastHP)
        {
            lastHP = playerController.currentHP;
        }

        float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
        anim.SetFloat("Speed", currentSpeed);

        anim.SetBool("IsGrounded", playerController.isGrounded);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }
}
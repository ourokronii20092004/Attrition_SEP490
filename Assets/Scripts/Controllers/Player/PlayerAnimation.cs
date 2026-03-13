using Fusion;
using UnityEngine;

public class PlayerAnimation : NetworkBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerController player;
    [SerializeField] private Rigidbody2D rb;

    public override void Spawned()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (player == null)
            player = GetComponent<PlayerController>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    public override void Render()
    {
        if (anim == null || player == null || rb == null) return;

        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("IsGrounded", player.IsGrounded);
        anim.SetBool("IsDead", player.IsDead);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }
}
using Fusion;
using UnityEngine;

public class PlayerAnimation : NetworkBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerController player;

    public override void Spawned()
    {
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (player == null)
            player = GetComponent<PlayerController>();
    }

    public override void Render()
    {
        if (anim == null || player == null) return;

        // TỐI ƯU TUYỆT ĐỐI: 
        // Đọc trực tiếp trạng thái từ mạng. Cực kỳ mượt, không bao giờ bị chạy nốt hay giật lag.
        anim.SetFloat("Speed", player.IsMoving ? 1f : 0f);
        anim.SetBool("IsGrounded", player.IsGrounded);
        anim.SetBool("IsDead", player.IsDead);
        anim.SetFloat("yVelocity", player.NetworkVelocityY);
    }
}
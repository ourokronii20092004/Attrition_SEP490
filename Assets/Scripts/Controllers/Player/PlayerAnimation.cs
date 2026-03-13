using Fusion;
using UnityEngine;

public class PlayerAnimation : NetworkBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerController player;

    private Vector3 _lastPos;

    // Thêm một biến để lưu trữ và làm mượt tốc độ
    private float _smoothedSpeed;

    public override void Spawned()
    {
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (player == null)
            player = GetComponent<PlayerController>();

        _lastPos = transform.position;
    }

    public override void Render()
    {
        if (anim == null || player == null) return;

        // 1. Đo khoảng cách di chuyển thực tế
        Vector3 visualVelocity = (transform.position - _lastPos) / Time.deltaTime;
        _lastPos = transform.position;

        // 2. Lấy tốc độ thô và lọc các chuyển động quá nhỏ
        float targetSpeed = Mathf.Abs(visualVelocity.x);
        if (targetSpeed < 0.1f) targetSpeed = 0f;

        // 3. LÀM MƯỢT TỐC ĐỘ (Đây là mấu chốt chống giật trên Client)
        // Lerp sẽ giúp thông số Speed hòa hoãn lại, không bị nhảy vọt hay tụt về 0 đột ngột do mạng lag
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, targetSpeed, Time.deltaTime * 15f);

        // 4. Gửi các thông số đã làm mượt vào Animator
        anim.SetFloat("Speed", _smoothedSpeed);
        anim.SetBool("IsGrounded", player.IsGrounded);
        anim.SetBool("IsDead", player.IsDead);
        anim.SetFloat("yVelocity", visualVelocity.y);
    }
}
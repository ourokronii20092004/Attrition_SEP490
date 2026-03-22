using Fusion;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private int _attackDamage = 1;
    public int attackDamage
    {
        get => _attackDamage;
        set => _attackDamage = value;
    }
    [Networked] public NetworkBool IsAttacking { get; set; }

    [Networked] private NetworkButtons _combatButtonsPrev { get; set; }
    private TickTimer attackCooldown;

    public override void Spawned()
    {
        if (player == null)
            player = GetComponent<PlayerController>();

        if (anim == null)
            anim = GetComponentInChildren<Animator>();
    }


    public override void FixedUpdateNetwork()
    {
        if (player == null || player.IsDead) return;

       
        if (attackPoint != null)
        {
            float sign = player.IsFacingRight ? 1f : -1f;
            attackPoint.localPosition = new Vector3(Mathf.Abs(attackPoint.localPosition.x) * sign, attackPoint.localPosition.y, attackPoint.localPosition.z);
        }

        if (GetInput(out NetworkInputData data))
        {
            var pressed = data.buttons.GetPressed(_combatButtonsPrev);

            if (pressed.IsSet(MyButtons.Attack) && attackCooldown.ExpiredOrNotRunning(Runner))
            {
                IsAttacking = true;
                attackCooldown = TickTimer.CreateFromSeconds(Runner, 0.5f);

                if (Runner.IsForward)
                {
                    RPC_PlayAttackAnimation();
                }
            }

            _combatButtonsPrev = data.buttons;
        }

        if (attackCooldown.Expired(Runner))
            IsAttacking = false;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PlayAttackAnimation()
    {
        if (anim != null)
            anim.SetTrigger("Attack");
    }

    public void TriggerAttackDamage()
    {
        // Máy dò 1: Xem Animation Event có thực sự gọi hàm này không?
        Debug.Log(">>> [PLAYER] Đã bóp cò vung kiếm!");

        if (!HasStateAuthority && !HasInputAuthority) return;
        if (attackPoint == null) { Debug.LogError("[PLAYER] Chưa gắn Attack Point!"); return; }

        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, targetLayers);

        // Máy dò 2: Xem vòng tròn có chạm vào cái gì không?
        Debug.Log($"[PLAYER] Vòng tròn quét trúng {hitTargets.Length} vật thể thuộc layer {LayerMask.LayerToName(targetLayers)}.");

        foreach (var target in hitTargets)
        {
            IDamageable dmg = target.GetComponent<IDamageable>();
            if (dmg != null)
            {
                Debug.Log($"[PLAYER] Đã chém trúng quái: {target.name}");
                dmg.TakeDamage(attackDamage);
                Vector2 pushDir = (target.transform.position - transform.position).normalized;
                pushDir = new Vector2(pushDir.x, 0.5f).normalized;
                dmg.TakeKnockback(pushDir, 5f);
            }
            else
            {
                Debug.LogWarning($"[PLAYER] Quét trúng {target.name} nhưng nó KHÔNG có script chứa IDamageable!");
            }
        }
    }
}
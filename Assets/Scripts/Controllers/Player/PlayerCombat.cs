using Fusion;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private int attackDamage = 1;

    [Networked] public NetworkBool IsAttacking { get; set; }
    private TickTimer attackCooldown;

    public override void Spawned()
    {
        if (player == null)
            player = GetComponent<PlayerController>();

        if (anim == null)
            anim = GetComponent<Animator>();
    }

    public override void FixedUpdateNetwork()
    {
        if (player == null) return;
        if (player.IsDead) return;

        if (GetInput(out NetworkInputData data))
        {
            if (data.buttons.WasPressed(data.buttons, MyButtons.Attack)
                && attackCooldown.ExpiredOrNotRunning(Runner))
            {
                IsAttacking = true;
                attackCooldown = TickTimer.CreateFromSeconds(Runner, 0.5f);

                RPC_PlayAttackAnimation();
            }
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
        if (!HasStateAuthority) return;

        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            targetLayers
        );

        foreach (var target in hitTargets)
        {
            IDamageable dmg = target.GetComponent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(attackDamage);
                Vector2 pushDir = (target.transform.position - transform.position).normalized;
                dmg.TakeKnockback(pushDir, 5f);
            }
        }
    }
}
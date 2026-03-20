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
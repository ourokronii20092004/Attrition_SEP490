using Fusion;
using UnityEngine;

public class EnemyAnimation : NetworkBehaviour
{
    [SerializeField] private Animator anim;
    private Vector3 originalScale;

    public override void Spawned()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>();
        originalScale = transform.localScale;
    }
    public void UpdateSpeed(float speed)
    {
        if (anim != null) anim.SetFloat("Speed", speed);
    }

    public void FaceTarget(float targetX)
    {
        float dirX = (targetX > transform.position.x) ? 1f : -1f;
        transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * dirX, originalScale.y, originalScale.z);
    }

    public void PlayAttack(int attackIndex)
    {
        if (anim != null)
        {
            anim.SetInteger("AttackIndex", attackIndex); 
            anim.SetTrigger("Attack");
        }
    }

    public void PlayHit() { if (anim != null) anim.SetTrigger("Hit"); }
    public void PlayDeath() { if (anim != null) anim.SetBool("IsDead", true); }
}
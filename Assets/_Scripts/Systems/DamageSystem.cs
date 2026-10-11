using System.Collections;
using UnityEngine;

public class DamageSystem : MonoBehaviour
{
    [SerializeField] private GameObject damageVFX;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DealDamageGA>(DealDamagePerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<DealDamageGA>();
    }
    
    private IEnumerator DealDamagePerformer(DealDamageGA dealDamageGA)
    {
        if (dealDamageGA == null || dealDamageGA.Targets == null)
            yield break;

        CombatantView firstTarget = null;

        foreach (var candidate in dealDamageGA.Targets)
        {
            if (candidate != null)
            {
                firstTarget = candidate;
                break;
            }
        }

        // Wait for the caster's attack lunge.
        if (dealDamageGA.Caster != null && firstTarget != null)
        {
            yield return dealDamageGA.Caster.PlayAttackLunge(
                firstTarget.transform.position
            );
        }

        foreach (var target in dealDamageGA.Targets)
        {
            if (target == null)
                continue;

            target.Damage(dealDamageGA.Amount);

            // Damage may have destroyed the target.
            if (target == null)
                continue;

            Instantiate(
                damageVFX,
                target.transform.position,
                Quaternion.identity
            );

            // Preserve your existing VFX display duration.
            yield return new WaitForSeconds(0.9f);

            // Check again after waiting.
            if (target == null)
                continue;

            if (target.CurrentHealth <= 0)
            {
                if (target is EnemyView enemyView)
                {
                    KillEnemyGA killEnemyGA = new(enemyView);
                    ActionSystem.Instance.AddReaction(killEnemyGA);
                }
                else
                {
                    // Keep your existing game-over logic here.
                }
            }
        }
    }

}

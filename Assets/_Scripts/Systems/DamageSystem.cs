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
        CombatantView firstTarget = null;
        foreach (var candidate in dealDamageGA.Targets)
        {
            if (candidate != null)
            {
                firstTarget = candidate;
                break;
            }
        }

        // Player and enemy both step in before the hit lands.
        if (dealDamageGA.Caster != null && firstTarget != null)
            yield return dealDamageGA.Caster.PlayAttackLunge(firstTarget.transform.position);

        foreach (var target in dealDamageGA.Targets)
        {
            // Guard against null or destroyed targets (e.g., enemy killed itself earlier)
            if (target == null) continue;

            target.Damage(dealDamageGA.Amount);
            // The target may have been destroyed by Damage, so verify before accessing transform
            if (target != null)
            {
                Instantiate(damageVFX, target.transform.position, Quaternion.identity);
                yield return new WaitForSeconds(0.9f);
            }

            if (target.CurrentHealth <= 0)
            {
                if (target is EnemyView enemyView)
                {
                    KillEnemyGA killEnemyGA = new(enemyView);
                    ActionSystem.Instance.AddReaction(killEnemyGA);
                }
                else
                {
                    //do some game over logic here
                }
            }
        }
    }
}

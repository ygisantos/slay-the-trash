using System.Collections;
using UnityEngine;

public class BurnSystem : MonoBehaviour
{
    [SerializeField] private GameObject burnVFX;
    private void OnEnable()
    {
        ActionSystem.AttachPerformer<ApplyBurnGA>(ApplyBurnPerformer);
    }
    private void OnDisable()
    {
        ActionSystem.DetachPerformer<ApplyBurnGA>();
    }

    private IEnumerator ApplyBurnPerformer(ApplyBurnGA applyBurnGA)
    {
        CombatantView target = applyBurnGA.Target;
        if (burnVFX != null && !burnVFX.name.ToLower().Contains("blood"))
            Instantiate(burnVFX, target.transform.position, Quaternion.identity);

        target.Damage(applyBurnGA.BurnDamage, true);
        target.RemoveStatusEffect(StatusEffectType.BURN, 1);

        if (target.CurrentHealth <= 0 && target is EnemyView enemyView)
            ActionSystem.Instance.AddReaction(new KillEnemyGA(enemyView));

        yield return target.WaitForDamageFeedback();
        yield return new WaitForSeconds(0.15f);
    }
}

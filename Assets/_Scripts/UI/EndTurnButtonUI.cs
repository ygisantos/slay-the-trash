using UnityEngine;

public class EndTurnButtonUI : MonoBehaviour
{
    public void OnClick()
    {
        if (CardSystem.Instance == null || !CardSystem.Instance.IsCombatActive) return;
        if (ActionSystem.Instance == null || ActionSystem.Instance.IsPerforming) return;

        EnemyTurnGA enemyTurnGA = new();
        ActionSystem.Instance.Perform(enemyTurnGA);
    }
}

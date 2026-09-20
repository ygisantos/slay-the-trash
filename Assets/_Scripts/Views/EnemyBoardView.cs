using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBoardView : MonoBehaviour
{
    [SerializeField] private List<Transform> slots;
    [SerializeField] private GameObject allClearedPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject scorePanel;
    public bool gameStart = false;
    private bool isWin = false;
    public List<EnemyView> EnemyViews { get; private set; } = new();

    private void Update()
    {
        if (gameStart)
        {
            LiveCheckForRemovedChildren();
            CheckIfBoardCleared();
        }
    }

    public void AddEnemy(EnemyData enemyData)
    {
        Transform slot = slots[EnemyViews.Count];
        EnemyView enemyView = EnemyViewCreator.Instance.CreateEnemyView(enemyData, slot.position, slot.rotation);
        enemyView.transform.parent = slot;
        EnemyViews.Add(enemyView);
    }

    public IEnumerator RemoveEnemy(EnemyView enemyView)
    {
        EnemyViews.Remove(enemyView);
        ScoreManager.Instance.AddEnemyKill();
        Tween tween = enemyView.transform.DOScale(Vector3.zero, 0.25f);
        yield return tween.WaitForCompletion();
        Destroy(enemyView.gameObject);
    }

    /// ✅ Checks every frame if any slot suddenly lost a child
    private void LiveCheckForRemovedChildren()
    {
        for (int i = EnemyViews.Count - 1; i >= 0; i--)
        {
            if (EnemyViews[i] == null || EnemyViews[i].transform == null || EnemyViews[i].transform.parent.childCount == 0)
            {
                // Remove immediately from list if missing
                EnemyViews.RemoveAt(i);
            }
        }
    }

    /// ✅ If no remaining enemies, show panel
    private void CheckIfBoardCleared()
    {
        bool anyChild = false;
        foreach (var slot in slots)
        {
            if (slot.childCount > 0)
            {
                anyChild = true;
                break;
            }
        }

        if (allClearedPanel != null)
            allClearedPanel.SetActive(!anyChild);
        if (scorePanel != null && MapManager.Instance.isBossLevel && !isWin)
        {
            if (!anyChild)
            {
                scorePanel.SetActive(true);
                isWin = true;
            }
        }
        if (winPanel != null && MapManager.Instance.isBossLevel)
            winPanel.SetActive(!anyChild);
        EnemyManager.Instance.isLevelCleared = anyChild;
        bool boardEmpty = !anyChild && EnemyViews.Count == 0;
        if (boardEmpty && gameStart)
        {
            gameStart = false;
            StartCoroutine(EndCombatWhenIdle());
        }
    }

    private IEnumerator EndCombatWhenIdle()
    {
        while (ActionSystem.Instance != null && ActionSystem.Instance.IsPerforming)
            yield return null;

        if (CardSystem.Instance != null)
            CardSystem.Instance.EndCombat();
    }
}

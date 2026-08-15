using UnityEngine;
using TMPro;

public class HealParticle : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI textMesh;

    [Header("FX Settings")]
    public float lifetime = 1.2f;
    public float floatSpeed = 1f;

    private float timer = 0f;
    private Transform txtTransform;

    private void Start()
    {
        if (textMesh != null)
            txtTransform = textMesh.transform;

        if (RewardManager.Instance != null && textMesh != null)
        {
            textMesh.text = RewardManager.Instance.rewardTxt;

            switch (RewardManager.Instance.lastRewardType)
            {
                case RewardType.AddHealth:
                    textMesh.color = Color.green;
                    break;
                case RewardType.AddShield:
                    textMesh.color = Color.cyan;
                    break;
                default:
                    textMesh.color = Color.white;
                    break;
            }
        }
    }

    private void Update()
    {
        if (txtTransform != null)
            txtTransform.position += Vector3.up * floatSpeed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifetime)
            Destroy(gameObject);
    }
}

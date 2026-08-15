using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapHandler : MonoBehaviour
{
    [System.Serializable]
    public struct LevelMaps
    {
        public List<MapView> maps;
    }

    [Header("Hierarchy")]
    public Transform mapParent;
    public Transform mainMap;

    [Header("Player Icon")]
    public RectTransform player;   // UI marker to show current map
    public GameObject healthPrefab;

    [Header("Generated Data")]
    public List<LevelMaps> levels = new List<LevelMaps>();
    [Header("Movement Settings")]
    public float glideDuration = 2f;
    public float scrollAmount = -600f; // how far mapParent moves right
    public float scrollDuration = 2f; // same as glide time
    public MatchSetupSystem matchSetupSystem;


    private void Start()
    {
        BuildMapLevels();
        LockAllLevelsExceptFirst();
    }

    // ---------------------------------------------------------
    // Build levels from hierarchy
    // ---------------------------------------------------------
    private void BuildMapLevels()
    {
        levels.Clear();

        for (int i = 0; i < mapParent.childCount; i++)
        {
            Transform levelObj = mapParent.GetChild(i);

            LevelMaps level = new LevelMaps
            {
                maps = new List<MapView>()
            };

            for (int j = 0; j < levelObj.childCount; j++)
            {
                Transform slot = levelObj.GetChild(j);
                MapView mv = slot.GetComponent<MapView>();
                if (mv == null)
                    mv = slot.GetComponentInChildren<MapView>();
                if (mv != null)
                {
                    MapData data = MapManager.Instance.GetMapData(i, j);
                    if (data != null)
                    {
                        mv.Setup(data);
                    }
                    level.maps.Add(mv);
                    mv.mapHandler = this;
                }
            }

            levels.Add(level);
        }
    }

    // ---------------------------------------------------------
    // Disable ALL tiles except first level
    // ---------------------------------------------------------
    public void LockAllLevelsExceptFirst()
    {
        LockAllLevels();

        // Enable only Level 0
        if (levels.Count > 0)
        {
            foreach (var mv in levels[0].maps)
            {
                mv.SetInteractable();
            }
        }
    }
    public void LockAllLevels()
    {
        for (int lvl = 0; lvl < levels.Count; lvl++)
        {
            foreach (var mv in levels[lvl].maps)
            {
                mv.SetNotInteractable();
            }
        }
    }

    // ---------------------------------------------------------
    // Move player icon to the selected map node
    // ---------------------------------------------------------
    public void MovePlayerTo(MapView mapView)
    {
        if (player == null || mapView == null) return;

        StopAllCoroutines();
        LockAllLevels();
        StartCoroutine(GlidePlayerAndScroll(mapView));
    }
    private IEnumerator GlidePlayerAndScroll(MapView target)
    {
        Vector3 startPos = player.position;
        Vector3 endPos = new Vector3(player.position.x, target.transform.position.y, player.position.z);

        Vector3 mapStart = mainMap.localPosition;
        Vector3 mapEnd = mapStart + new Vector3(scrollAmount, 0f, 0f);

        float time = 0f;

        while (time < glideDuration)
        {
            float t = time / glideDuration;

            // Smooth animation
            t = Mathf.SmoothStep(0f, 1f, t);

            // Move player
            player.position = Vector3.Lerp(startPos, endPos, t);

            // Scroll map parent
            mainMap.localPosition = Vector3.Lerp(mapStart, mapEnd, t);

            time += Time.deltaTime;
            yield return null;
        }

        // Final positions
        player.position = endPos;
        mainMap.localPosition = mapEnd;
        MapViewLogic(target);
    }
    public void MapViewLogic(MapView mapView)
    {
        if(mapView == null) return;
        switch(mapView.mapName)
        {
            case "Enemy":
                if (MapManager.Instance.level == 1)
                {
                    matchSetupSystem.StartGame();
                }
                else
                {
                    matchSetupSystem.EnemyWaves();
                }
                break;
            case "Event":
                QuestionManager.Instance.QuestionActivate();
                break;
            case "Heal":
                RewardManager.Instance.GiveRandomReward();
                SpawnHealthAtPlayer();
                break;
            case "Boss":
                matchSetupSystem.EnemyWaves();
                MapManager.Instance.isBossLevel = true;
                break;
            default:
                Debug.Log(mapView.mapName);
                break;
        }
        LockAllLevels();
        foreach (MapView mv in mapView.nextMapNode)
        {
            mv.SetInteractable();
        }
    }
    public void SpawnHealthAtPlayer()
    {
        if (player == null || healthPrefab == null)
        {
            Debug.LogWarning("Player or HealthPrefab is missing!");
            return;
        }

        // Instantiate at player world position
        Instantiate(healthPrefab, player.position, Quaternion.identity,transform);
    }

}

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class MapView : MonoBehaviour
{
    [Header("Data")]
    public MapData mapData;

    [Header("UI")]
    public Button mapButton;
    public Image mapImage;

    [Header("Settings")]
    public float alphaUndone = 1f;
    public float alphaDone = 0.5f;

    [HideInInspector] public MapHandler mapHandler;

    public List<MapView> nextMapNode = new List<MapView>();

    // Runtime state (NOT in MapData)
    private bool isDone = false;
    [HideInInspector] public string mapName;

    public void Setup(MapData data)
    {
        mapData = data;

        // Reset runtime state
        isDone = false;

        // Set main sprite
        if (mapImage != null)
            mapImage.sprite = data.mainImage;

        // Reset visuals
        UpdateAlpha();

        mapButton.onClick.RemoveAllListeners();
        mapButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        mapButton.interactable = false;
        MapManager.Instance.level++;
        mapName = mapData.type.ToString();
        if (mapImage != null)
            mapHandler.MovePlayerTo(this);
    }

    public void SetNotInteractable()
    {
        mapButton.interactable = false;
        SetToDone();
    }
    public void SetInteractable()
    {
        mapButton.interactable = true;
        isDone = false;
        UpdateAlpha();
    }

    public void SetToDone()
    {
        isDone = true;
        UpdateAlpha();
    }

    private void UpdateAlpha()
    {
        float alpha = isDone ? alphaDone : alphaUndone;

        if (mapImage != null)
        {
            var c = mapImage.color;
            c.a = alpha;
            mapImage.color = c;
        }
    }
}

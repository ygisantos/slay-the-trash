using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class LogController : MonoBehaviour
{
    [SerializeField]
    private bool enableLogging = true;

    void Awake() => UpdateDebugLogging();
    [Button]
    public void UpdateDebugLogging() => Debug.unityLogger.logEnabled = enableLogging;
}
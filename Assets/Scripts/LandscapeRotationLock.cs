using UnityEngine;

public class LandscapeRotationLock : MonoBehaviour
{
    void Start()
    {
        // Enable auto-rotation for landscape only
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        
        // Disable portrait orientations
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        
        // Apply the auto-rotation setting
        Screen.orientation = ScreenOrientation.AutoRotation;
    }
}

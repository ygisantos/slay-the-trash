using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    // Change scene by name
    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneChanger: Scene name is empty!");
            return;
        }

        // Load scene
        SceneManager.LoadScene(sceneName);
    }
}

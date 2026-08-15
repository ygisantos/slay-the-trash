using UnityEngine;

public class CharacterSelector : MonoBehaviour
{
    [System.Serializable]
    public class CharacterInfo
    {
        public string characterName;
        public UnityEngine.Events.UnityEvent onConfirm;   // 2nd tap event
    }

    [Header("Character Info (4 total)")]
    public CharacterInfo[] characters;

    [Header("Character Image Objects (4 total)")]
    public GameObject[] characterImages;
    // Only 1 image shows when tapped

    private int currentIndex = -1;
    private bool waitingForConfirm = false;

    // Called by buttons
    public void OnCharacterButtonPressed(int index)
    {
        // FIRST TAP — show image
        if (currentIndex != index || !waitingForConfirm)
        {
            currentIndex = index;
            waitingForConfirm = true;
            SoundManager.PlaySound(SoundType.CLICK);

            ShowCharacterImage(index);
            return;
        }

        // SECOND TAP — confirm
        waitingForConfirm = false;
        SoundManager.PlaySound(SoundType.CLICK);

        // Save selected character
        SelectedCharacter.index = index;

        // Trigger inspector event
        characters[index].onConfirm?.Invoke();
    }

    void ShowCharacterImage(int index)
    {
        for (int i = 0; i < characterImages.Length; i++)
        {
            characterImages[i].SetActive(i == index);
        }
    }

    // 🔥 You asked to keep this — it stays!
    public void ConfirmCharacter(int i)
    {
        SelectedCharacter.index = i;
    }

    void Start()
    {
        // Hide all character images at start
        for (int i = 0; i < characterImages.Length; i++)
        {
            characterImages[i].SetActive(false);
        }
    }
}

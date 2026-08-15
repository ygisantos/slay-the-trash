using UnityEngine;
using UnityEngine.UI;

public class CharacterSwapper : MonoBehaviour
{
    public enum CharacterType { Witch, Knight, Hunter, Cleric };

    public GameObject[] players;
    public GameObject[] uiImage;
    public GameObject[] mapImage;
    public GameObject[] questionImage;
    public GameObject[] questionAnswer;
    public GameObject[] scoreCanva;

    public int charN;

    void Start()
    {
        charN = SelectedCharacter.index;
        SwitchCharacter(charN);
    }

    public void SetCharacter(int charNum)
    {
        if (charNum != charN && charNum >= 0 && charNum < players.Length)
        {
            charN = charNum;
            SwitchCharacter(charNum);
        }
    }

    public void SwitchCharacter(int charNum)
    {
        if (charNum < 0 || charNum >= players.Length) return;

        // ──────────────────────────────────
        // 1) Deactivate ALL first
        // ──────────────────────────────────
        for (int i = 0; i < players.Length; i++)
        {
            players[i].SetActive(false);
            uiImage[i].SetActive(false);
            mapImage[i].SetActive(false);
            questionImage[i].SetActive(false);
            questionAnswer[i].SetActive(false);
            scoreCanva[i].SetActive(false);
        }

        // ──────────────────────────────────
        // 2) Activate only selected character
        // ──────────────────────────────────
        players[charNum].SetActive(true);
        uiImage[charNum].SetActive(true);
        mapImage[charNum].SetActive(true);
        questionImage[charNum].SetActive(true);
        questionAnswer[charNum].SetActive(true);
        scoreCanva[charNum].SetActive(true);
    }
}

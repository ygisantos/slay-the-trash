using UnityEngine;

public class MenuAnimationController : MonoBehaviour
{
    public Animator animator;


    public void OpenPlay()
    {
        SoundManager.Instance.PlaySound(SoundType.CLICK);
        animator.SetTrigger("Play_Open");
    }

    public void ClosePlay()
    {
        SoundManager.Instance.PlaySound(SoundType.CLICK);
        animator.SetTrigger("Play_Close");
    }

    public void OpenSettings()
    {
        SoundManager.Instance.PlaySound(SoundType.CLICK);
        animator.SetTrigger("Settings_Open");
    }

    public void CloseSettings()
    {
        SoundManager.Instance.PlaySound(SoundType.CLICK);
        animator.SetTrigger("Settings_Close");
    }

    public void OpenQuit()
    {
        SoundManager.Instance.PlaySound(SoundType.CLICK);
        animator.SetTrigger("Quit_Open");
    }

    public void CloseQuit()
    {
        SoundManager.Instance.PlaySound(SoundType.CLICK);
        animator.SetTrigger("Quit_Close");
    }
}

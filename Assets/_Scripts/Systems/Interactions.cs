
using UnityEngine;

public class Interactions : Singleton<Interactions>
{
    public bool PlayerIsDragging { get; set; } = false;

    public bool PlayerCanInteract()
    {
        return ActionSystem.Instance != null
            && !ActionSystem.Instance.IsPerforming;
    }

    public bool PlayerCanHover()
    {
        if (PlayerIsDragging)
            return false;

        return PlayerCanInteract();
    }
}

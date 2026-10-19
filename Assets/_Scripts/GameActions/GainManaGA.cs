using UnityEngine;

public class GainManaGA : GameAction
{
    public int Amount { get; set; }

    public GainManaGA(int amount)
    {
        Amount = amount;
    }
}


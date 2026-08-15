using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Card Pool")]
public class CardPool : ScriptableObject
{
    public List<CardPoolEntry> cards = new List<CardPoolEntry>();
}

[Serializable]
public struct CardPoolEntry
{
    public string Name;
    public int Amount;

    public CardPoolEntry(string name, int amount)
    {
        Name = name;
        Amount = amount;
    }
}

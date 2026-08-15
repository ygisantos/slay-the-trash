using System;
using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
    Paper,
    Plastic,
    BioWaste,
    Draw
}
public enum CardRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(menuName = "Data/Card Library")]
public class CardLibrary : ScriptableObject
{
    public List<NamedCard> cards = new List<NamedCard>();
}

[Serializable]
public struct NamedCard
{
    public string Name;
    public CardData Data;
    public CardType Type;
    public CardRarity Rarity;

    public NamedCard(string name, CardData data, CardType type, CardRarity rarity)
    {
        Name = name;
        Data = data;
        Type = type;
        Rarity = rarity;
    }
}

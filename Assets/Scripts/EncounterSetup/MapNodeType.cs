using UnityEngine;

public enum MapNodeType
{
    Hidden,
    Start,
    Encounter,
    Shop,
    RandomEvent,
    RemoveTwoCards,
    DuplicateCard,
    Boss
}

[System.Serializable]
public class MapNodeTypeWeights
{
    [Tooltip("Weight for Encounter nodes")]
    public int encounterWeight = 55;

    [Tooltip("Weight for Shop nodes")]
    public int shopWeight = 15;

    [Tooltip("Weight for Random Event nodes")]
    public int randomEventWeight = 10;

    [Tooltip("Weight for Remove 2 Cards nodes")]
    public int removeTwoCardsWeight = 10;

    [Tooltip("Weight for Duplicate Card nodes")]
    public int duplicateCardWeight = 10;

    public MapNodeType GetRandomNodeType()
    {
        int totalWeight =
            encounterWeight +
            shopWeight +
            randomEventWeight +
            removeTwoCardsWeight +
            duplicateCardWeight;

        int randomValue = Random.Range(0, totalWeight);

        if (randomValue < encounterWeight)
            return MapNodeType.Encounter;

        randomValue -= encounterWeight;
        if (randomValue < shopWeight)
            return MapNodeType.Shop;

        randomValue -= shopWeight;
        if (randomValue < randomEventWeight)
            return MapNodeType.RandomEvent;

        randomValue -= randomEventWeight;
        if (randomValue < removeTwoCardsWeight)
            return MapNodeType.RemoveTwoCards;

        return MapNodeType.DuplicateCard;
    }
}
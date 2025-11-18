using UnityEngine;

public enum MapNodeType
{
    Encounter,
    Shop,
    RandomEvent,
    Boss
}

[System.Serializable]
public class MapNodeTypeWeights
{
    [Tooltip("Weight for Encounter nodes (higher = more common)")]
    public int encounterWeight = 70;
    
    [Tooltip("Weight for Shop nodes")]
    public int shopWeight = 15;
    
    [Tooltip("Weight for Random Event nodes")]
    public int randomEventWeight = 15;

    public MapNodeType GetRandomNodeType()
    {
        int totalWeight = encounterWeight + shopWeight + randomEventWeight;
        int randomValue = Random.Range(0, totalWeight);

        if (randomValue < encounterWeight)
            return MapNodeType.Encounter;
        else if (randomValue < encounterWeight + shopWeight)
            return MapNodeType.Shop;
        else
            return MapNodeType.RandomEvent;
    }
}

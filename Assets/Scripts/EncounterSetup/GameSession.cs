// Assets/Scripts/Map/GameSession.cs
using UnityEngine;
using Chess;

public class GameSession : MonoBehaviour
{
    public static GameSession I { get; private set; }

    [Header("Content")]
    public EncounterCatalog catalog;

    [HideInInspector] public EncounterDefinition selectedEncounter;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; } //make sure there's only one
        I = this;
        DontDestroyOnLoad(gameObject); //stays on throughout scenes so it can be called upon
    }
    
    //randomly chooses premade encounter
    public EncounterDefinition PickRandomEncounter()
    {
        if (catalog == null || catalog.encounters == null || catalog.encounters.Count == 0) return null;
        return catalog.encounters[Random.Range(0, catalog.encounters.Count)];
    }
}
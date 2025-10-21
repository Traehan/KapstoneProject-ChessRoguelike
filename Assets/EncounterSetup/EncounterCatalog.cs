// Assets/Scripts/Map/EncounterCatalog.cs
using System.Collections.Generic;
using UnityEngine;
using Chess;

[CreateAssetMenu(menuName = "Chess/Encounters/Catalog", fileName = "EncounterCatalog")]
public class EncounterCatalog : ScriptableObject
{
    public List<EncounterDefinition> encounters = new();
}
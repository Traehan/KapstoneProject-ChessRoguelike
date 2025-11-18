using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using GameManager;
using Chess;
using UnityEngine.SceneManagement;

public class RunMapController : MonoBehaviour
{
    [Header("Map Generator")]
    public MapGenerator mapGenerator;

    [Header("Legacy UI Buttons (Optional)")]
    public Button leftButton;
    public Button rightButton;

    [Header("Scenes")]
    [Tooltip("Your board/GameRoot scene (was SampleScene).")]
    public string battleSceneName = "SampleScene";

    [Tooltip("Battle HUD scene to load additively.")]
    public string uiSceneName = "UI_Battle";

    void Awake()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        if (leftButton != null)
        {
            leftButton.onClick.AddListener(OnBranchClicked);
        }
        
        if (rightButton != null)
        {
            rightButton.onClick.AddListener(OnBranchClicked);
        }
    }

    void OnBranchClicked()
    {
        var gs = GameSession.I;
        if (gs == null)
        {
            Debug.LogError("GameSession missing in MapScene");
            return;
        }

        var chosen = gs.PickRandomEncounter();
        if (chosen == null)
        {
            Debug.LogError("EncounterCatalog is empty");
            return;
        }

        gs.selectedEncounter = chosen;
        StartCoroutine(LoadBattleFlow(chosen));
    }

    IEnumerator LoadBattleFlow(EncounterDefinition encounter)
    {
        yield return SceneController.instance.GoTo(battleSceneName, encounter);
    }

    public void ResetMapForNewRun()
    {
        if (mapGenerator != null)
        {
            mapGenerator.ResetMap();
        }
        else
        {
            Debug.LogWarning("MapGenerator not found!");
        }
    }
}
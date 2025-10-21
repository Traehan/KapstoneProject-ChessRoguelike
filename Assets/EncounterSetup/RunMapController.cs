// Assets/Scripts/Map/RunMapController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using GameManager;        // <-- your SceneController namespace
using Chess;
using UnityEngine.SceneManagement;

public class RunMapController : MonoBehaviour
{
    [Header("UI")] public Button leftButton;
    public Button rightButton;

    [Header("Scenes")] [Tooltip("Your board/GameRoot scene (was SampleScene).")]
    public string battleSceneName = "SampleScene";

    [Tooltip("Battle HUD scene to load additively.")]
    public string uiSceneName = "UI_Battle";

    void Awake()
    {
        leftButton.onClick.AddListener(OnBranchClicked);
        rightButton.onClick.AddListener(OnBranchClicked);
    }

    void OnBranchClicked()
    {
        // 1) Pick a random encounter from your catalog via GameSession
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

        // (Optional) Keep this for older code paths
        gs.selectedEncounter = chosen;

        // 2) Hand off via SceneArgs.Payload (strongest guarantee)
        //    EncounterRunner in SampleScene will read this in Start()
        StartCoroutine(LoadBattleFlow(chosen));
    }

    IEnumerator LoadBattleFlow(EncounterDefinition encounter)
    {
        // swap to SampleScene, passing the encounter as args
        yield return SceneController.instance.GoTo(battleSceneName, encounter);
    }
}
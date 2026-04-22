using UnityEngine;
using UnityEngine.SceneManagement;
using Chess;

public class DevMapTools : MonoBehaviour
{
    [Header("Enable")]
    [SerializeField] bool enableInEditor = true;
    [SerializeField] bool enableInDevelopmentBuild = true;

    [Header("Scene Restriction")]
    [SerializeField] string requiredSceneName = "MapScene";

    [Header("Input")]
    [SerializeField] KeyCode toggleOverlayKey = KeyCode.F1;

    [Header("Clan Dev Options")]
    [SerializeField] ClanDefinition[] devClanOptions;

    bool _visible;
    Rect _windowRect = new Rect(20f, 20f, 360f, 320f);

    void Update()
    {
        if (!DevToolsAllowed()) return;
        if (!InRequiredScene()) return;

        if (Input.GetKeyDown(toggleOverlayKey))
            _visible = !_visible;
    }

    bool DevToolsAllowed()
    {
        if (Application.isEditor && enableInEditor)
            return true;

        if (Debug.isDebugBuild && enableInDevelopmentBuild)
            return true;

        return false;
    }

    bool InRequiredScene()
    {
        if (string.IsNullOrWhiteSpace(requiredSceneName))
            return true;

        return SceneManager.GetActiveScene().name == requiredSceneName;
    }

    void OnGUI()
    {
        if (!DevToolsAllowed()) return;
        if (!InRequiredScene()) return;
        if (!_visible) return;

        _windowRect = GUI.Window(99127, _windowRect, DrawWindow, "Dev Tools");
    }

    void DrawWindow(int windowId)
    {
        GUILayout.BeginVertical();

        var gs = GameSession.I;
        var map = FindObjectOfType<MapGenerator>();

        GUILayout.Label($"Current Clan: {gs?.selectedClan?.clanName ?? "None"}");
        GUILayout.Label($"Map Pos: {gs?.mapCurrentRow}, {gs?.mapCurrentColumn}");
        GUILayout.Space(8);

        if (map != null)
        {
            if (GUILayout.Button("Jump To Boss Battle Now", GUILayout.Height(30f)))
                map.DevJumpToBossBattleNow();

            if (GUILayout.Button("Jump To Boss Node Only", GUILayout.Height(26f)))
                map.DevJumpToBossNodeOnly();
        }
        else
        {
            GUILayout.Label("MapGenerator not found.");
        }

        GUILayout.Space(10);
        GUILayout.Label("Clan Swap");

        if (devClanOptions != null)
        {
            for (int i = 0; i < devClanOptions.Length; i++)
            {
                var clan = devClanOptions[i];
                if (clan == null) continue;

                if (GUILayout.Button($"Swap To: {clan.clanName}", GUILayout.Height(26f)))
                {
                    if (GameSession.I != null)
                    {
                        GameSession.I.DevSwapClanMidRun(clan, grantRandomStartingTroop: true);
                        Debug.Log($"[DevMapTools] Swapped run to {clan.clanName}");
                    }
                }
            }
        }

        GUILayout.Space(8);

        if (GUILayout.Button("Hide", GUILayout.Height(24f)))
            _visible = false;

        GUILayout.EndVertical();
        GUI.DragWindow();
    }
}
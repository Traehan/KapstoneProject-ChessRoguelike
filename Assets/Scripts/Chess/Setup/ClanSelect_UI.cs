using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // fallback
using GameManager;
using Chess;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Rendering;

public class ClanSelectUI : MonoBehaviour
{
    [System.Serializable]
    public class ClanButtonConfig
    {
        public ClanDefinition clan;
        public Button button;
    }

    [Header("Data + Buttons")]
    [Tooltip("Each entry links a ClanDefinition to a specific button already placed in the scene.")]
    public List<ClanButtonConfig> clanButtons = new List<ClanButtonConfig>();

    [Header("UI")]
    public Button startRunButton;

    [Header("Scenes")]
    public string mapSceneName = "MapScene";

    ClanDefinition _chosen;
    
    public TextMeshProUGUI clanNameText;
    public TextMeshProUGUI clanPassiveText;
    public TextMeshProUGUI queenAuraText;
    public Image queenIcon;
    

    void Start()
    {
        if (startRunButton == null)
        {
            Debug.LogError("[ClanSelectUI] StartRunButton not assigned.");
            return;
        }

        if (clanButtons == null || clanButtons.Count == 0)
        {
            Debug.LogError("[ClanSelectUI] No clanButtons configured. Assign buttons + clans in the inspector.");
            return;
        }
        
        //set image false for aesthetic until image is called
        queenIcon.gameObject.SetActive(false);
        
        // Setup Start Run button
        startRunButton.interactable = false;
        startRunButton.onClick.RemoveAllListeners();
        startRunButton.onClick.AddListener(OnStartRun);

        // Wire each preset button to its clan
        foreach (var config in clanButtons)
        {
            if (config == null)
                continue;

            if (config.button == null)
            {
                Debug.LogWarning("[ClanSelectUI] ClanButtonConfig has no button assigned.");
                continue;
            }

            if (config.clan == null)
            {
                Debug.LogWarning("[ClanSelectUI] ClanButtonConfig has no clan assigned.");
                continue;
            }

            var def = config.clan;       // local copy for closure
            var btn = config.button;

            // Optional: auto-set label text from clan name
            var label = def ? def.clanName : "Unknown Clan";

            // TMP or legacy text — support both
            var tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp) tmp.text = label;
            else
            {
                var legacy = btn.GetComponentInChildren<Text>();
                if (legacy) legacy.text = label;
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnPick(def));
        }

        Debug.Log("[ClanSelectUI] Ready. Click a clan button, then Start.");
    }

    void OnPick(ClanDefinition def)
    {
        _chosen = def;
        startRunButton.interactable = (_chosen != null);
        Debug.Log($"[ClanSelectUI] Picked clan: {(_chosen ? _chosen.clanName : "NULL")}");
        clanNameText.text = _chosen.clanName;
        clanPassiveText.text = _chosen.ClanPassiveDescription;
        queenAuraText.text = _chosen.QueenAuraDescription;
        queenIcon.gameObject.SetActive(true);
        queenIcon.sprite = _chosen.Queen;
        
    }

    void OnStartRun()
    {
        if (_chosen == null)
        {
            Debug.LogWarning("[ClanSelectUI] Start clicked but no clan selected.");
            return;
        }

        if (GameSession.I == null)
        {
            Debug.LogError("[ClanSelectUI] No GameSession in scene! Create a GameObject with GameSession.cs (DontDestroyOnLoad).");
            // You can still go to map scene, but the run won’t have state:
            SafeGoToMap();
            return;
        }

        GameSession.I.StartNewRun(_chosen);
        Debug.Log("[ClanSelectUI] New run started. Loading map...");
        SafeGoToMap();
    }

    void SafeGoToMap()
    {
        if (string.IsNullOrEmpty(mapSceneName))
        {
            Debug.LogError("[ClanSelectUI] mapSceneName not set.");
            return;
        }

        if (SceneController.instance != null)
            SceneController.instance.GoTo(mapSceneName);
        else
        {
            Debug.LogWarning("[ClanSelectUI] SceneController.instance is null. Using SceneManager.LoadScene fallback.");
            SceneManager.LoadScene(mapSceneName);
        }
    }
}

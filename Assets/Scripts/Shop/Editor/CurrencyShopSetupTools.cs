using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class CurrencyShopSetupTools : EditorWindow
{
    [MenuItem("Tools/Shop System/Setup Currency Manager")]
    static void SetupCurrencyManager()
    {
        GameObject existing = GameObject.Find("CurrencyManager");
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Already Exists",
                "CurrencyManager already exists in the scene!",
                "OK");
            Selection.activeGameObject = existing;
            return;
        }

        GameObject currencyManager = new GameObject("CurrencyManager");
        currencyManager.AddComponent<CurrencyManager>();

        EditorUtility.SetDirty(currencyManager);

        EditorUtility.DisplayDialog("Success",
            "CurrencyManager created!\n\n" +
            "This GameObject will persist across scenes (DontDestroyOnLoad).\n\n" +
            "Configure the settings in the Inspector:\n" +
            "- Starting Coins: 0\n" +
            "- Encounter Victory Reward: 75",
            "OK");

        Selection.activeGameObject = currencyManager;
    }

    [MenuItem("Tools/Shop System/Add Currency Display to Scene")]
    static void AddCurrencyDisplay()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No Canvas found in scene!\n" +
                "Please create a Canvas first.",
                "OK");
            return;
        }

        GameObject coinDisplay = new GameObject("CurrencyDisplay");
        coinDisplay.transform.SetParent(canvas.transform, false);

        RectTransform rt = coinDisplay.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20, -20);
        rt.sizeDelta = new Vector2(200, 50);

        var text = coinDisplay.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "Coins: 0";
        text.fontSize = 24;
        text.color = Color.white;
        text.fontStyle = TMPro.FontStyles.Bold;

        var display = coinDisplay.AddComponent<CurrencyDisplay>();

        EditorUtility.SetDirty(coinDisplay);

        EditorUtility.DisplayDialog("Success",
            "Currency Display created at top-left!\n\n" +
            "It will automatically connect to CurrencyManager and update.",
            "OK");

        Selection.activeGameObject = coinDisplay;
    }

    [MenuItem("Tools/Shop System/Add Victory Reward Handler")]
    static void AddVictoryRewardHandler()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "SampleScene")
        {
            EditorUtility.DisplayDialog("Wrong Scene",
                "Please open SampleScene (the battle scene) first!",
                "OK");
            return;
        }

        GameObject handler = new GameObject("VictoryRewardHandler");
        handler.AddComponent<VictoryRewardHandler>();

        EditorUtility.SetDirty(handler);

        EditorUtility.DisplayDialog("Success",
            "Victory Reward Handler created!\n\n" +
            "This will automatically award coins when the player wins a battle.\n" +
            "It connects to TurnManager.OnPlayerWon event.",
            "OK");

        Selection.activeGameObject = handler;
    }

    [MenuItem("Tools/Shop System/Show Setup Guide")]
    static void ShowSetupGuide()
    {
        string guidePath = "Assets/Scripts/Shop/ShopScene_SetupGuide.txt";
        TextAsset guide = AssetDatabase.LoadAssetAtPath<TextAsset>(guidePath);
        
        if (guide != null)
        {
            Selection.activeObject = guide;
            EditorGUIUtility.PingObject(guide);
        }
        else
        {
            EditorUtility.DisplayDialog("Not Found",
                "Setup guide not found at:\n" + guidePath,
                "OK");
        }
    }

    [MenuItem("Tools/Shop System/Create Upgrade Template")]
    static void CreateUpgradeTemplate()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Piece Upgrade",
            "NewUpgrade",
            "asset",
            "Choose where to save the new upgrade"
        );

        if (string.IsNullOrEmpty(path))
            return;

        Chess.PieceUpgradeSO upgrade = ScriptableObject.CreateInstance<Chess.PieceUpgradeSO>();
        upgrade.displayName = "New Upgrade";
        upgrade.description = "Describe what this upgrade does";
        upgrade.addMaxHP = 0;
        upgrade.addAttack = 0;

        AssetDatabase.CreateAsset(upgrade, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = upgrade;
        EditorGUIUtility.PingObject(upgrade);

        EditorUtility.DisplayDialog("Success",
            "Upgrade template created!\n\n" +
            "Configure it in the Inspector:\n" +
            "- Display Name\n" +
            "- Description\n" +
            "- Icon\n" +
            "- Add Max HP\n" +
            "- Add Attack",
            "OK");
    }

    [MenuItem("Tools/Shop System/Open Currency & Shop Summary")]
    static void OpenSummary()
    {
        string summaryPath = "Assets/Scripts/Shop/CURRENCY_AND_SHOP_SUMMARY.txt";
        TextAsset summary = AssetDatabase.LoadAssetAtPath<TextAsset>(summaryPath);
        
        if (summary != null)
        {
            Selection.activeObject = summary;
            EditorGUIUtility.PingObject(summary);
        }
        else
        {
            EditorUtility.DisplayDialog("Not Found",
                "Summary not found at:\n" + summaryPath,
                "OK");
        }
    }
}

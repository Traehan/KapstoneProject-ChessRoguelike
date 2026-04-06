using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

public class MapPrefabCreator : EditorWindow
{
    [MenuItem("Tools/Map System/Create Map Node Prefabs")]
    static void CreatePrefabs()
    {
        CreateMapNodeVisualPrefab();

        EditorUtility.DisplayDialog(
            "Success",
            "Map node prefab created successfully!\n\n" +
            "Created:\n" +
            "- MapNodeVisual.prefab\n\n" +
            "Check /Assets/Prefabs folder",
            "OK");
    }

    static void CreateMapNodeVisualPrefab()
    {
        GameObject nodeObj = new GameObject("MapNodeVisual");

        RectTransform rectTransform = nodeObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);

        Image bgImage = nodeObj.AddComponent<Image>();
        bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bgImage.type = Image.Type.Sliced;
        bgImage.color = Color.white;

        Button button = nodeObj.AddComponent<Button>();
        button.targetGraphic = bgImage;

        MapNodeVisual nodeVisual = nodeObj.AddComponent<MapNodeVisual>();

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(nodeObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(60, 60);
        iconRect.anchoredPosition = new Vector2(0, 10);

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        iconImage.color = Color.white;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(nodeObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0);
        textRect.anchoredPosition = new Vector2(0, -40);
        textRect.sizeDelta = new Vector2(0, 30);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Battle";
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        nodeVisual.backgroundImage = bgImage;
        nodeVisual.iconImage = iconImage;
        nodeVisual.nodeTypeText = text;

        string prefabPath = "Assets/Prefabs";
        if (!Directory.Exists(prefabPath))
            Directory.CreateDirectory(prefabPath);

        string fullPath = prefabPath + "/MapNodeVisual.prefab";
        PrefabUtility.SaveAsPrefabAsset(nodeObj, fullPath);

        DestroyImmediate(nodeObj);

        Debug.Log("Created MapNodeVisual prefab at: " + fullPath);
    }

    [MenuItem("Tools/Map System/Setup Map Scene")]
    static void SetupMapScene()
    {
        GameObject mapObj = GameObject.Find("Map");
        if (mapObj == null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Could not find 'Map' GameObject in the scene.\nPlease open MapScene first.",
                "OK");
            return;
        }

        MapGenerator generator = mapObj.GetComponent<MapGenerator>();
        if (generator == null)
            generator = mapObj.AddComponent<MapGenerator>();

        GameObject nodeVisualPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MapNodeVisual.prefab");

        if (nodeVisualPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "MapNodeVisual prefab not found.\n\nPlease create it first using:\n" +
                "Tools > Map System > Create Map Node Prefabs",
                "OK");
            return;
        }

        Transform scrollView = mapObj.transform.Find("Scroll View");
        Transform content = null;

        if (scrollView != null)
        {
            Transform viewport = scrollView.Find("Viewport");
            if (viewport != null)
                content = viewport.Find("Content");
        }

        if (content == null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Could not find Content object at:\n" +
                "Map/Scroll View/Viewport/Content\n\n" +
                "Please verify your scene hierarchy.",
                "OK");
            return;
        }

        generator.nodeVisualPrefab = nodeVisualPrefab;
        generator.contentParent = content.GetComponent<RectTransform>();

        // New chessboard-map defaults
        generator.boardWidth = 5;
        generator.playableRows = 8;
        generator.horizontalSpacing = 200f;
        generator.verticalSpacing = 150f;
        generator.battleSceneName = "SampleScene";
        generator.shopSceneName = "ShopScene";
        generator.eventSceneName = "EventScene";

        EditorUtility.SetDirty(mapObj);

        EditorUtility.DisplayDialog(
            "Success",
            "MapGenerator configured successfully!\n\n" +
            "Settings applied:\n" +
            "- MapNodeVisual prefab assigned\n" +
            "- Content parent linked\n" +
            "- Board width = 5\n" +
            "- Playable rows = 8\n\n" +
            "Press Play to test the new chess map.",
            "OK");
    }
}
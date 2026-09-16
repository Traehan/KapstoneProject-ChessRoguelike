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

    // "Setup Map Scene" used to live here: it auto-wired the old ScrollRect/UI-based MapGenerator
    // fields (nodeVisualPrefab/contentParent/horizontalSpacing/verticalSpacing), which were removed
    // when the map moved to a 3D world-space camera/tile system (see Map3D/MapGenerator.cs). The new
    // mapTilePrefab/mapTilesRoot/mapClanToken/mapCameraRig fields are wired directly in MapScene.unity
    // instead - removed this menu command rather than leave it calling fields that no longer exist.
}
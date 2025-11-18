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
        CreatePathLinePrefab();
        
        EditorUtility.DisplayDialog("Success", 
            "Map Node prefabs created successfully!\n\n" +
            "Created:\n" +
            "- MapNodeVisual.prefab\n" +
            "- PathLine.prefab\n\n" +
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
        iconObj.transform.SetParent(nodeObj.transform);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(60, 60);
        iconRect.anchoredPosition = new Vector2(0, 10);
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        iconImage.color = Color.white;
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(nodeObj.transform);
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
        {
            Directory.CreateDirectory(prefabPath);
        }
        
        string fullPath = prefabPath + "/MapNodeVisual.prefab";
        PrefabUtility.SaveAsPrefabAsset(nodeObj, fullPath);
        
        DestroyImmediate(nodeObj);
        
        Debug.Log("Created MapNodeVisual prefab at: " + fullPath);
    }

    static void CreatePathLinePrefab()
    {
        GameObject lineObj = new GameObject("PathLine");
        
        RectTransform rectTransform = lineObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 4);
        
        Image lineImage = lineObj.AddComponent<Image>();
        lineImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        
        string prefabPath = "Assets/Prefabs";
        if (!Directory.Exists(prefabPath))
        {
            Directory.CreateDirectory(prefabPath);
        }
        
        string fullPath = prefabPath + "/PathLine.prefab";
        PrefabUtility.SaveAsPrefabAsset(lineObj, fullPath);
        
        DestroyImmediate(lineObj);
        
        Debug.Log("Created PathLine prefab at: " + fullPath);
    }

    [MenuItem("Tools/Map System/Setup Map Scene")]
    static void SetupMapScene()
    {
        GameObject mapObj = GameObject.Find("Map");
        if (mapObj == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "Could not find 'Map' GameObject in the scene.\n" +
                "Please open MapScene first.", 
                "OK");
            return;
        }

        MapGenerator generator = mapObj.GetComponent<MapGenerator>();
        if (generator == null)
        {
            generator = mapObj.AddComponent<MapGenerator>();
        }

        GameObject nodeVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MapNodeVisual.prefab");
        GameObject pathLinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PathLine.prefab");

        if (nodeVisualPrefab == null || pathLinePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "Prefabs not found. Please create them first using:\n" +
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
            {
                content = viewport.Find("Content");
            }
        }

        if (content == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "Could not find Content object at:\n" +
                "Map/Scroll View/Viewport/Content\n\n" +
                "Please verify your scene hierarchy.", 
                "OK");
            return;
        }

        generator.nodeVisualPrefab = nodeVisualPrefab;
        generator.pathLinePrefab = pathLinePrefab;
        generator.contentParent = content.GetComponent<RectTransform>();
        generator.numberOfRows = 10;
        generator.minNodesPerRow = 2;
        generator.maxNodesPerRow = 4;
        generator.horizontalSpacing = 200f;
        generator.verticalSpacing = 150f;
        generator.battleSceneName = "SampleScene";

        EditorUtility.SetDirty(mapObj);
        
        EditorUtility.DisplayDialog("Success", 
            "MapGenerator configured successfully!\n\n" +
            "Settings applied:\n" +
            "- Prefabs assigned\n" +
            "- Content parent linked\n" +
            "- Default values set\n\n" +
            "Press Play to test the map!", 
            "OK");
    }
}

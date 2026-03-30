using UnityEditor;

public static class ReserializeURPAssets
{
    [MenuItem("Tools/Reserialize URP Assets")]
    public static void Run()
    {
        AssetDatabase.ForceReserializeAssets(new[]
        {
            "Assets/Settings/PC_RPAsset.asset",
            "Assets/Settings/PC_Renderer.asset"
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        UnityEngine.Debug.Log("Reserialized PC_RPAsset and PC_Renderer.");
    }
}
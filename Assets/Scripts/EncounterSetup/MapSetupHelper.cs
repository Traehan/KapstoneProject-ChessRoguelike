using UnityEngine;

public class MapSetupHelper : MonoBehaviour
{
    [Header("Instructions")]
    [TextArea(5, 10)]
    public string instructions = 
        "This helper script provides instructions for setting up the Map Node system.\n\n" +
        "1. Create a Prefab for the Node Visual:\n" +
        "   - Create a UI Button GameObject\n" +
        "   - Add an Image component for background\n" +
        "   - Add a TextMeshPro text for node type label\n" +
        "   - Add an Image component for icon\n" +
        "   - Add MapNodeVisual script\n" +
        "   - Save as Prefab in /Assets/Prefabs\n\n" +
        "2. Create a Prefab for Path Lines:\n" +
        "   - Create a UI Image GameObject\n" +
        "   - Set it as a thin horizontal line\n" +
        "   - Save as Prefab in /Assets/Prefabs\n\n" +
        "3. Attach MapGenerator to the Map GameObject\n" +
        "4. Assign the Content transform from Scroll View\n" +
        "5. Assign both prefabs to MapGenerator\n" +
        "6. Configure MapGenerator settings";
}

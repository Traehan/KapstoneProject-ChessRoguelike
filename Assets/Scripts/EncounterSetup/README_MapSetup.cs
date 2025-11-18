/*
 * ========================================
 * MAP NODE SYSTEM - SETUP GUIDE
 * ========================================
 * 
 * This guide explains how to set up the vertical scrolling map node system in MapScene.
 * 
 * ========================================
 * SCRIPTS CREATED:
 * ========================================
 * 1. MapNodeType.cs - Defines node types (Encounter, Shop, RandomEvent)
 * 2. MapNode.cs - Core data class for a single map node
 * 3. MapNodeVisual.cs - UI component for displaying nodes
 * 4. MapGenerator.cs - Generates and manages the map
 * 5. MapState.cs - Saves/loads map state between scenes
 * 6. Updated RunMapController.cs - Integrates with new system
 * 7. Updated GameSession.cs - Clears map state on new run
 * 
 * ========================================
 * SETUP INSTRUCTIONS:
 * ========================================
 * 
 * STEP 1: CREATE NODE VISUAL PREFAB
 * ----------------------------------
 * 1. In Hierarchy, right-click and create: UI > Button
 * 2. Rename it to "MapNodeVisual"
 * 3. Select MapNodeVisual and configure:
 *    - RectTransform: Width = 100, Height = 100
 *    - Add component: MapNodeVisual script
 * 4. Create child objects:
 *    a) Right-click MapNodeVisual > UI > Image (rename to "Icon")
 *       - Position: Y = 10
 *       - Size: 60 x 60
 *    b) Right-click MapNodeVisual > UI > Text - TextMeshPro (rename to "Text")
 *       - Position: Y = -40
 *       - Size: Stretch horizontally, Height = 30
 *       - Text alignment: Center
 *       - Font size: 18
 * 5. Select MapNodeVisual, in MapNodeVisual script component:
 *    - Drag the Button's Image to "Background Image"
 *    - Drag Icon Image to "Icon Image"
 *    - Drag Text to "Node Type Text"
 * 6. Drag MapNodeVisual from Hierarchy to /Assets/Prefabs folder
 * 7. Delete MapNodeVisual from Hierarchy
 * 
 * STEP 2: CREATE PATH LINE PREFAB
 * --------------------------------
 * 1. In Hierarchy, right-click and create: UI > Image
 * 2. Rename it to "PathLine"
 * 3. Configure:
 *    - RectTransform: Width = 100, Height = 4
 *    - Image Color: White or light gray
 * 4. Drag PathLine from Hierarchy to /Assets/Prefabs folder
 * 5. Delete PathLine from Hierarchy
 * 
 * STEP 3: CONFIGURE MAP GAMEOBJECT
 * ---------------------------------
 * 1. In MapScene Hierarchy, select the "Map" GameObject
 * 2. Add component: MapGenerator script
 * 3. Configure MapGenerator settings:
 *    - Number Of Rows: 10
 *    - Min Nodes Per Row: 2
 *    - Max Nodes Per Row: 4
 *    - Node Type Weights:
 *      * Encounter Weight: 70 (most common)
 *      * Shop Weight: 15
 *      * Random Event Weight: 15
 * 4. Visual Settings:
 *    - Node Visual Prefab: Drag MapNodeVisual prefab here
 *    - Content Parent: Drag "Map/Scroll View/Viewport/Content" here
 *    - Horizontal Spacing: 200
 *    - Vertical Spacing: 150
 * 5. Path Lines:
 *    - Path Line Prefab: Drag PathLine prefab here
 * 6. Scene References:
 *    - Battle Scene Name: "SampleScene"
 *    - Shop Scene Name: "ShopScene" (create later)
 *    - Event Scene Name: "EventScene" (create later)
 * 
 * STEP 4: CONFIGURE RunMapController
 * -----------------------------------
 * 1. In MapScene Hierarchy, select the "Map" GameObject
 * 2. Locate the RunMapController component
 * 3. Configure:
 *    - Map Generator: Drag the Map GameObject here (or leave empty, it auto-finds)
 *    - Battle Scene Name: "SampleScene"
 * 
 * STEP 5: TEST THE MAP
 * --------------------
 * 1. Make sure GameSession prefab is in the scene or loaded at startup
 * 2. Press Play
 * 3. You should see a vertical scrolling map with nodes
 * 4. The first row of nodes should be clickable (white)
 * 5. Other nodes should be locked (gray)
 * 6. Click a node to:
 *    - Mark it as visited
 *    - Lock the entire row
 *    - Unlock connected nodes in next row
 *    - Navigate to the appropriate scene
 * 
 * ========================================
 * HOW IT WORKS:
 * ========================================
 * 
 * MAP GENERATION:
 * - On first load, MapGenerator creates 10 rows of nodes
 * - Each row has 2-4 nodes randomly
 * - Each node is randomly assigned: Encounter (70%), Shop (15%), or Event (15%)
 * - Nodes are connected with branching paths
 * - Paths can merge back together
 * 
 * NODE INTERACTION:
 * - Only nodes in the current available row can be clicked
 * - When a node is clicked:
 *   1. The node is marked as visited (turns gray)
 *   2. All other nodes in that row are locked
 *   3. Connected nodes in the next row become available
 *   4. The map state is saved to PlayerPrefs
 *   5. The appropriate scene is loaded:
 *      - Encounter → SampleScene (battle)
 *      - Shop → ShopScene (not implemented yet)
 *      - Event → EventScene (not implemented yet)
 * 
 * MAP STATE PERSISTENCE:
 * - Map state is saved to PlayerPrefs after each node click
 * - When returning to MapScene, the same map layout is restored
 * - You continue from where you left off
 * - To reset: GameSession.StartNewRun() clears the map state
 * 
 * PATH LOCKING:
 * - Once you choose a path, you're locked into following it
 * - You can only click nodes connected to your previously clicked node
 * - This creates meaningful choices in the roguelike progression
 * 
 * ========================================
 * CUSTOMIZATION OPTIONS:
 * ========================================
 * 
 * To change node appearance:
 * - Edit MapNodeVisual prefab
 * - Adjust colors in MapNodeVisual script component
 * - Assign custom sprites for encounterSprite, shopSprite, eventSprite
 * 
 * To change map generation:
 * - Adjust numberOfRows, minNodesPerRow, maxNodesPerRow
 * - Modify nodeTypeWeights to change encounter/shop/event ratios
 * - Edit ConnectNodes() in MapGenerator.cs for different branching logic
 * 
 * To add new node types:
 * - Add to MapNodeType enum
 * - Update MapNodeTypeWeights.GetRandomNodeType()
 * - Add case in MapGenerator.NavigateToNodeScene()
 * - Add corresponding colors/sprites in MapNodeVisual
 * 
 * ========================================
 * TROUBLESHOOTING:
 * ========================================
 * 
 * "Nodes aren't appearing":
 * - Check that nodeVisualPrefab and contentParent are assigned
 * - Verify MapNodeVisual prefab has MapNodeVisual script
 * - Check Console for error messages
 * 
 * "Nodes aren't clickable":
 * - Verify first row nodes have isCurrentlyAvailable = true
 * - Check that Button component is enabled
 * - Ensure EventSystem exists in scene
 * 
 * "Map doesn't persist":
 * - MapState uses PlayerPrefs - check it's saving correctly
 * - Verify MapGenerator.SaveMapState() is being called
 * 
 * "Navigation to scenes fails":
 * - Ensure SceneController.instance exists
 * - Check scene names match in Build Settings
 * - Verify GameSession.I exists with encounterCatalog assigned
 * 
 * ========================================
 */

using UnityEngine;

public class README_MapSetup : MonoBehaviour
{
}

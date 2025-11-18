using GameManager;
using UnityEngine;

namespace Chess
{
    public class ContinueToMapButton : MonoBehaviour
    {
        [SerializeField] string mapSceneName = "MapScene"; // change if your scene is named differently
        public void Go()
        {
            var controller = FindObjectOfType<SceneController>();
            if (controller != null)
                
                controller.GoTo(mapSceneName);
            else
                Debug.LogWarning("SceneController not found!");
        }
    }
}
using GameManager;
using UnityEngine;

namespace Chess
{
    public class MainMenuButton : MonoBehaviour
    {
        public void GoToMainMenu() //made this a separate script so it can be implemented on different buttons throughout the game
        {
            var controller = FindObjectOfType<SceneController>();
            if (controller != null)
                controller.GoTo("StartScreen");
            else
                Debug.LogWarning("SceneController not found in scene!");
        }
    }
}

